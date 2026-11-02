# GPT-6.1 Terra with Microsoft Agent Framework — C# Sample

[![build](https://github.com/taswar/GPT-6-1-Terra-AgentFramework-CSharp-Sample/actions/workflows/dotnet.yml/badge.svg)](https://github.com/taswar/GPT-6-1-Terra-AgentFramework-CSharp-Sample/actions/workflows/dotnet.yml)
![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)
[![License: MIT](https://img.shields.io/badge/license-MIT-green)](LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen)](CONTRIBUTING.md)

- 📖 Full walkthrough: [GPT-6.1 Terra for C# Developers](https://taswar.zeytinsoft.com/gpt-6-1-terra-csharp-developers-guide/?utm_source=github&utm_medium=readme&utm_campaign=GPT-6-1-Terra-AgentFramework-CSharp-Sample&utm_content=walkthrough)
- ⭐ If this sample helps you, star the repo so other .NET developers can find it.
- 🔁 Related: [GPT-6.1 Terra C# Sample (HttpClient)](https://github.com/taswar/GPT-6-1-Terra-CSharp-Sample), the same agent with hand-written requests
- For more C# samples using other AI models, see [AI models for C# developers](https://taswar.zeytinsoft.com/foundry-ai-models-csharp/?utm_source=github&utm_medium=readme&utm_campaign=GPT-6-1-Terra-AgentFramework-CSharp-Sample&utm_content=ai-models-hub)
- For my free ebook on prompt engineering, see [Prompt Engineering for .NET Developers](https://github.com/taswar/PromptEngineeringForDotNetDevelopers)

A small .NET 10 console application that reviews a PDF supplier invoice using an Azure OpenAI deployment and Microsoft Agent Framework. It sends the invoice to the Responses API, gives the agent a local supplier lookup tool, and receives a typed `InvoiceReview` result.

The default deployment name is `gpt-6.1-terra`. Set `AZURE_OPENAI_DEPLOYMENT` to the name of your own deployment if it differs.

## What the sample demonstrates

- Connecting the OpenAI .NET Responses client to Azure's `/openai/v1` endpoint.
- Creating an `AIAgent` with instructions and a function tool using `AsAIAgent`.
- Sending a PDF as `DataContent` with the `application/pdf` media type.
- Registering `check_supplier` with an explicit tool name and strict schema settings.
- Requesting structured output through `RunAsync<InvoiceReview>`.
- Setting low reasoning effort through `CreateResponseOptions`.
- Printing the review, token usage, and elapsed time.

The agent is instructed to call `check_supplier` with the supplier name printed on the invoice. It flags invoices for approval when the total is **over 5,000**, the supplier is missing from the vendor master, or the supplier is inactive. It also lists issues such as incorrect arithmetic or a missing purchase order number.

These rules are agent instructions; this sample does not implement a separate deterministic approval check or submit invoices for payment. Review its output before using it in an accounts payable workflow.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- An Azure OpenAI resource with a deployment that supports the Responses API, PDF input, function calling, structured output, and the reasoning options used here.
- Either an Azure OpenAI API key or an Azure identity with permission to invoke the deployment.
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli) if using `az login` for local authentication.

Running the sample sends the invoice to your configured Azure service and incurs model usage charges. Use sample or suitably approved invoice data.

## Quick start

### 1. Clone and restore

```powershell
git clone https://github.com/taswar/GPT-6-1-Terra-AgentFramework-CSharp-Sample.git
cd GPT-6-1-Terra-AgentFramework-CSharp-Sample
dotnet restore
```

### 2. Configure your Azure resource

The project already has a `UserSecretsId`, so you can set local configuration without changing source files:

```powershell
dotnet user-secrets set "AZURE_OPENAI_ENDPOINT" "https://YOUR-RESOURCE-NAME.openai.azure.com"
dotnet user-secrets set "AZURE_OPENAI_DEPLOYMENT" "gpt-6.1-terra"
```

Use the resource's base URL for `AZURE_OPENAI_ENDPOINT`. The application appends `/openai/v1`; do not include that suffix in your setting.

Choose one authentication option:

**Microsoft Entra ID**

```powershell
az login
```

Leave `AZURE_OPENAI_API_KEY` unset or empty. The application uses `DefaultAzureCredential` with the `https://ai.azure.com/.default` scope. For local development, this can use your Azure CLI sign-in. Your identity needs appropriate data-plane permissions, such as the **Cognitive Services OpenAI User** role on the resource.

**API key**

```powershell
dotnet user-secrets set "AZURE_OPENAI_API_KEY" "YOUR-API-KEY"
```

A nonempty API key takes precedence over identity authentication and is sent in the `api-key` header. To switch back to identity authentication, remove it from user secrets and any environment variables:

```powershell
dotnet user-secrets remove "AZURE_OPENAI_API_KEY"
Remove-Item Env:AZURE_OPENAI_API_KEY -ErrorAction SilentlyContinue
```

### 3. Run the invoice review

From the project directory, use the included `invoice.pdf`:

```powershell
dotnet run
```

Or pass a path to another invoice:

```powershell
dotnet run -- "C:\invoices\supplier-invoice.pdf"
```

The default PDF path is relative to the current working directory. The PDF is read at runtime and is not copied into the build output by the project.

The console prints supplier details, invoice number and date, purchase order number, total and currency, approval status, and any issues. It also logs supplier tool calls and a usage line containing input, cached input, output, and reasoning token counts plus elapsed seconds. Usage fields may be empty if the response does not provide them.

## Configuration

| Setting | Required | Behavior |
| --- | --- | --- |
| `AZURE_OPENAI_ENDPOINT` | Yes | Azure resource base URL; trailing slashes are trimmed. |
| `AZURE_OPENAI_DEPLOYMENT` | No | Deployment name; defaults to `gpt-6.1-terra`. |
| `AZURE_OPENAI_API_KEY` | No | Nonempty value enables API key authentication; otherwise uses `DefaultAzureCredential`. |

Environment variables override user secrets. For example, in PowerShell:

```powershell
$env:AZURE_OPENAI_ENDPOINT = "https://YOUR-RESOURCE-NAME.openai.azure.com"
$env:AZURE_OPENAI_DEPLOYMENT = "gpt-6.1-terra"
dotnet run
```

The application loads user secrets and environment variables. It does not automatically load `.env` or `appsettings.json` files. User secrets are a local development convenience and are not encrypted; use an appropriate secret store for deployed applications.

## How it works

1. Read configuration and create a `ResponsesClient` for the Azure v1 endpoint.
2. Create the `InvoiceReviewer` agent with invoice review instructions and the `check_supplier` function.
3. Read the PDF and send it with the prompt `Review this invoice.`
4. Run the agent with low reasoning effort and a structured output schema generated from the `InvoiceReview` record. The framework handles function invocation and deserializes the final result.
5. Print the result and usage information.

The supplier lookup is an in-memory stand-in for an ERP or vendor master:

| Supplier | Vendor ID | Status |
| --- | --- | --- |
| Fabrikam Office Supplies | `V-1042` | `active` |
| Northwind Traders | `V-0877` | `on hold` |

Lookup is case-insensitive and trims leading and trailing whitespace. Other names return `Found = false`. Replace `CheckSupplier` with your own lookup when adapting the sample.

`InvoiceReview` contains `SupplierName`, `SupplierOnFile`, `InvoiceNumber`, `InvoiceDate`, `PoNumber`, `Currency`, `Total`, `RequiresApproval`, and `Issues`. The schema describes dates as ISO 8601, currency codes as ISO 4217, and a missing purchase order as an empty string.

## Project files

| File | Purpose |
| --- | --- |
| [Program.cs](Program.cs) | Configuration, client setup, agent, PDF input, supplier tool, and output records. |
| [GPT-6-1-Terra-AgentFramework-CSharp-Sample.csproj](GPT-6-1-Terra-AgentFramework-CSharp-Sample.csproj) | .NET 10 target and NuGet package references. |
| [invoice.pdf](invoice.pdf) | Included invoice for trying the sample. |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Development and pull request guidance. |
| [LICENSE](LICENSE) | MIT license. |

The direct dependencies are `Azure.Identity`, `Microsoft.Agents.AI.OpenAI`, and the environment variable and user secrets configuration providers. See the project file for pinned versions. The code suppresses `OPENAI001` because the OpenAI Responses types used here are marked experimental.

## Troubleshooting

- **Missing endpoint:** Set `AZURE_OPENAI_ENDPOINT` through user secrets or environment variables.
- **PDF not found:** Run from the project directory or pass an existing PDF path after `--`.
- **Authentication or authorization error:** Check your API key, or sign in with `az login` and verify your identity's permissions on the resource. A nonempty configured API key overrides identity authentication.
- **Deployment not found:** Set `AZURE_OPENAI_DEPLOYMENT` to your actual deployment name and verify the resource endpoint.
- **Unsupported API or model option:** Verify that the deployment supports the PDF input, tools, structured output, and low reasoning effort used by this sample.
- **Unexpected review result:** Inspect the invoice, tool log, and agent instructions. The lookup uses only the two sample suppliers above, and the review is model-generated.

## Contributing and license

Contributions are welcome. See [CONTRIBUTING.md](CONTRIBUTING.md) for setup and validation guidance.

Licensed under the [MIT License](LICENSE), copyright (c) 2026 Taswar.
