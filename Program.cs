#pragma warning disable OPENAI001 // The OpenAI Responses types are marked experimental.

using System.ClientModel;
using System.ClientModel.Primitives;
using System.ComponentModel;
using System.Diagnostics;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Responses;

var config = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .AddEnvironmentVariables()
    .Build();

var endpoint = (config["AZURE_OPENAI_ENDPOINT"]
    ?? throw new InvalidOperationException(
        "Set AZURE_OPENAI_ENDPOINT with dotnet user-secrets, for example https://YOUR-RESOURCE-NAME.openai.azure.com"))
    .TrimEnd('/');
var deployment = config["AZURE_OPENAI_DEPLOYMENT"] ?? "gpt-6.1-terra";

// The OpenAI SDK talks to Azure through the v1 endpoint, so no Azure-specific client is needed.
var clientOptions = new ResponsesClientOptions { Endpoint = new Uri($"{endpoint}/openai/v1") };

// The API key is optional. Without one, the app signs in with Entra ID through az login.
var apiKey = config["AZURE_OPENAI_API_KEY"];
var responsesClient = string.IsNullOrWhiteSpace(apiKey)
    ? new ResponsesClient(
        authenticationPolicy: new BearerTokenPolicy(new DefaultAzureCredential(), "https://ai.azure.com/.default"),
        options: clientOptions)
    : new ResponsesClient(
        authenticationPolicy: ApiKeyAuthenticationPolicy.CreateHeaderApiKeyPolicy(new ApiKeyCredential(apiKey), "api-key"),
        options: clientOptions);

// The agent: Terra on the Responses API, with instructions and one tool.
AIAgent agent = responsesClient
    .AsAIAgent(
        model: deployment,
        name: "InvoiceReviewer",
        instructions: """
            You review supplier invoices for accounts payable.
            Read the attached invoice. Always call check_supplier with the supplier name exactly as printed.
            It requires approval if the total is over 5000, or if the supplier is not on file or not active.
            List anything a reviewer should look at as issues, such as math that doesn't add up or a missing PO number.
            Use an empty list when there is nothing to flag.
            """,
        tools:
        [
            // Name the tool yourself: a local function's generated name looks like _Main_g_CheckSupplier_0_0.
            // "strict" asks the OpenAI adapter to send the tool schema in strict mode.
            AIFunctionFactory.Create(CheckSupplier, new AIFunctionFactoryOptions
            {
                Name = "check_supplier",
                AdditionalProperties = new Dictionary<string, object?> { ["strict"] = true }
            })
        ]);

var pdfPath = args.Length > 0 ? args[0] : "invoice.pdf";
if (!File.Exists(pdfPath))
{
    throw new FileNotFoundException("Run from the project folder, or pass the path to a PDF invoice.", pdfPath);
}

// The PDF goes in as DataContent. The OpenAI adapter sends application/pdf as an input_file.
var message = new ChatMessage(ChatRole.User,
[
    new DataContent(await File.ReadAllBytesAsync(pdfPath), "application/pdf") { Name = Path.GetFileName(pdfPath) },
    new TextContent("Review this invoice.")
]);

// Reasoning effort has no ChatOptions property, so it goes on the raw Responses options.
var options = new ChatClientAgentRunOptions(new ChatOptions
{
    AdditionalProperties = new AdditionalPropertiesDictionary { ["strict"] = true },
    RawRepresentationFactory = _ => new CreateResponseOptions
    {
        ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = ResponseReasoningEffortLevel.Low }
    }
});

var stopwatch = Stopwatch.StartNew();

// RunAsync<T> asks for structured output with a schema built from InvoiceReview, runs the tool calls,
// and deserializes the final answer.
AgentResponse<InvoiceReview> response = await agent.RunAsync<InvoiceReview>(message, options: options);
stopwatch.Stop();

var review = response.Result;
Console.WriteLine();
Console.WriteLine($"Supplier:       {review.SupplierName} (on file: {review.SupplierOnFile})");
Console.WriteLine($"Invoice:        {review.InvoiceNumber}, dated {review.InvoiceDate}, PO {review.PoNumber}");
Console.WriteLine($"Total:          {review.Total:N2} {review.Currency}");
Console.WriteLine($"Needs approval: {review.RequiresApproval}");
foreach (var issue in review.Issues)
{
    Console.WriteLine($"Issue:          {issue}");
}

var usage = response.Usage;
Console.WriteLine();
Console.WriteLine($"[terra] input={usage?.InputTokenCount} cached={usage?.CachedInputTokenCount} output={usage?.OutputTokenCount} reasoning={usage?.ReasoningTokenCount} elapsed={stopwatch.Elapsed.TotalSeconds:0.0}s");

// A stand-in for your ERP or vendor master. Replace it with a real lookup.
// The framework builds the tool schema from the method signature and the Description attributes.
[Description("Looks up a supplier in the vendor master. Returns whether it exists, its ID and its status.")]
static SupplierLookup CheckSupplier([Description("The supplier name as printed on the invoice.")] string supplierName)
{
    var suppliers = new Dictionary<string, (string Id, string Status)>(StringComparer.OrdinalIgnoreCase)
    {
        ["Fabrikam Office Supplies"] = ("V-1042", "active"),
        ["Northwind Traders"] = ("V-0877", "on hold")
    };

    var result = suppliers.TryGetValue(supplierName.Trim(), out var supplier)
        ? new SupplierLookup(true, supplier.Id, supplier.Status)
        : new SupplierLookup(false, null, null);

    Console.WriteLine($"[tool] CheckSupplier(\"{supplierName}\") -> {result}");
    return result;
}

record SupplierLookup(bool Found, string? SupplierId, string? Status);

// The C# shape of the structured output. The framework turns it into the JSON schema.
record InvoiceReview(
    string SupplierName,
    bool SupplierOnFile,
    string InvoiceNumber,
    [property: Description("ISO 8601 date, for example 2026-10-12.")] string InvoiceDate,
    [property: Description("Empty if the invoice has no PO number.")] string PoNumber,
    [property: Description("ISO 4217 code, for example USD.")] string Currency,
    decimal Total,
    bool RequiresApproval,
    List<string> Issues);
