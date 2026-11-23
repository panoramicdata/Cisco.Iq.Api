# Agent instructions

## Glossary

- NET: .NET, Microsoft's developer platform; this is a product name.
- IQ: Cisco IQ, Cisco's support and professional services platform; this is a product name.

## Identity and scope

Act as a Microsoft .NET maintainer of the Cisco IQ REST client. Preserve documented wire formats
and keep public operations consistent with request objects and explicit cancellation tokens.

## Scope and boundaries

- Do not weaken `TreatWarningsAsErrors`, or bypass CI/CD checks.
- Keep credentials outside source control and redact tokens from output.
- Preserve assertions and coverage thresholds. If a changed API contract requires an adjustment,
  explain the reason and obtain reviewer approval.
- Keep live integration tests separate from unit coverage runs, and change that arrangement only
  when the user asks.
- Publish packages only when the user has authorized publication.
- Do not force-push to `main`, rewrite published history, or delete branches without explicit
  approval.
- Do not add `Co-Authored-By` trailers or other AI attribution to commit messages or pull requests.

## Tools and verification

- Use `rg` to search the repository, `dotnet build --configuration Release` to compile,
  and `dotnet test --configuration Release --filter "Category!=Integration" --fail-skips on`
  to verify unit tests.
- Use `git diff --check` before committing.
- CI enforces 100% line and branch coverage with `tools/Assert-Coverage.ps1`.

## Repository conventions

This repository provides the Cisco IQ .NET API client. Target .NET 11.0 and manage NuGet
versions centrally in `Directory.Packages.props`.

Skipped unit tests must fail; if a test cannot run, report the missing prerequisite instead of skipping it. Integration tests use the `Category=Integration` trait and
require Cisco IQ credentials; keep credentials out of source control.

CI collects standard and extended Cobertura coverage using `coverage.config` and
`coverage.settings.xml` and enforces the thresholds in `tools/Assert-Coverage.ps1`.
Preserve JSON property names and converters when changing response models.

## Example

When adding an operation, define a typed request and take an explicit cancellation token:

```csharp
Task<IResponse<Asset>> GetAssetAsync(GetAssetRequest request, CancellationToken cancellationToken);
```

## PanoramicData staff

PanoramicData staff have shared tooling in a private sibling checkout of PanoramicData.Skills. Anyone
without it can ignore this section: everything needed to work on this repository is above.