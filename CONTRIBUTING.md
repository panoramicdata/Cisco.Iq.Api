# Contributing

Use .NET 10 and the SDK selection in global.json. Package versions are managed in
Directory.Packages.props; use xUnit v3, AwesomeAssertions and the Microsoft.Testing.Platform
coverage collector. Keep C# namespaces file scoped and indentation as tabs.

Run `dotnet build --configuration Release` and `dotnet test --configuration Release`.
Integration tests skip when CiscoIq:Token is absent. Store credentials in user secrets
or environment variables, never in source or fixtures. Integration requests are read-only
and hit production; keep page sizes and request counts small.

Run coverage using the commands in README.md. Changes should preserve 100% line coverage.
Generated Refit clients, logging and regular expression implementations are excluded;
the extended coverage configuration includes handwritten async method bodies.

Use a branch and submit a pull request against main. Publish.ps1 is the release workflow;
run it only when the change is approved for publication.
