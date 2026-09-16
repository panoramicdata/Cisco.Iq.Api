# Repository conventions

This repository provides the Cisco IQ .NET API client. Target .NET 10 and manage NuGet
versions centrally in `Directory.Packages.props`.

Build with `dotnet build --configuration Release`. Run unit tests with
`dotnet test --configuration Release --filter "Category!=Integration" --fail-skips on`.
Skipped unit tests must fail. Integration tests use the `Category=Integration` trait and
require Cisco IQ credentials; keep credentials out of source control.

CI collects standard and extended Cobertura coverage using `coverage.config` and
`coverage.settings.xml` and enforces the thresholds in `tools/Assert-Coverage.ps1`.
Preserve JSON property names and converters when changing response models.
