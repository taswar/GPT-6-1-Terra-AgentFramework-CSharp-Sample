# Contributing

Thanks for contributing to this C# invoice review sample. Keep changes focused on making the Microsoft Agent Framework integration easy to understand and run.

## Getting started

1. Fork the repository and clone your fork.
2. Install the .NET 10 SDK.
3. Create a branch for your change:

   ```powershell
   git switch -c your-change-name
   ```

4. Restore and build from the project directory:

   ```powershell
   dotnet restore
   dotnet build --no-restore
   ```

5. For a live run, configure your Azure endpoint, deployment, and authentication as described in [README.md](README.md), then run `dotnet run`.

Building requires package access but does not require Azure credentials. Running the invoice review requires a compatible Azure deployment and incurs model usage charges.

## Making changes

- Follow the existing C# style, including nullable reference types, top-level statements, and records for structured results.
- Keep the sample small and readable. Explain nonobvious SDK or framework behavior in concise comments.
- Preserve explicit tool naming and verify schema compatibility when changing tool signatures or output records.
- Update the README when changing setup, configuration, dependencies, or observable behavior.
- Avoid unrelated refactoring and dependency updates in the same pull request.
- Keep credentials, local configuration, real supplier information, and confidential invoices out of commits, logs, issues, and screenshots. Use synthetic data for examples.

## Validation

Before opening a pull request, run:

```powershell
dotnet restore
dotnet build --no-restore
git diff --check
```

There is currently no automated test project. For changes to agent behavior, tools, authentication, or PDF handling, also perform a live run with a suitable sample invoice when you have access to a deployment:

```powershell
dotnet run -- "path\to\sample-invoice.pdf"
```

Check that the supplier tool is called, the result deserializes into `InvoiceReview`, and the review and usage information are printed. For changes to approval instructions, check examples covering an active supplier, an inactive or unknown supplier, and totals at and above 5,000. The rule is strictly **over** 5,000. Model output can vary, so report the observed behavior rather than relying on an exact text match.

If a live run is unavailable, state that in the pull request and list the checks you did complete. Add focused automated tests when a change introduces logic that can usefully be tested without a live model call.

## Pull requests

Open a pull request against the repository's default branch. Include:

- The problem being addressed and the resulting behavior.
- Any setup or compatibility changes reviewers need to know about.
- The validation performed, including whether a live Azure run was completed.
- A related issue link, if applicable.

Use a descriptive title and keep the diff focused. Do not include generated `bin/` or `obj/` files. For larger changes, open an issue first to discuss the proposed direction.

## Reporting issues

Use [GitHub issues](https://github.com/taswar/GPT-6-1-Terra-AgentFramework-CSharp-Sample/issues) for bugs and feature requests. For a bug, include reproduction steps, expected and actual behavior, your .NET SDK version, and a sanitized error message. Include deployment and package details when relevant, but remove credentials and sensitive invoice contents.

For suspected security vulnerabilities or exposed credentials, avoid posting sensitive details in a public issue. Use GitHub's private vulnerability reporting feature if it is enabled, or contact the maintainer through an available private channel. Revoke any exposed credentials promptly.

## License

By submitting a contribution, you agree that it may be distributed under this repository's [MIT License](LICENSE).
