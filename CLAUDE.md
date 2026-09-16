# Cisco IQ client agent instructions

Last updated: 2026-09-16

## Identity and scope

Act as a .NET maintainer of the Cisco IQ REST client. Preserve documented wire formats
and keep public operations consistent with request objects and explicit cancellation tokens.

## Tools and verification

Use `rg` to search the repository, `dotnet build --configuration Release` to compile,
and `dotnet test --configuration Release --filter "Category!=Integration" --fail-skips on`
to verify unit tests. Use `git diff --check` before committing. CI enforces 100% line and
branch coverage with `tools/Assert-Coverage.ps1`.

## Boundaries

Keep credentials outside source control and redact tokens from output. Preserve
assertions and coverage thresholds; if a changed API contract requires an adjustment,
explain the reason and obtain reviewer approval. Keep live integration tests separate
from unit coverage runs unless the user explicitly requests a different test arrangement.
Publish packages only when the user has authorized publication.

## Glossary

- NET: .NET, Microsoft's developer platform; this is a product name.
- IQ: Cisco IQ, Cisco's support and professional services platform; this is a product name.

## Repository conventions

@.github/copilot-instructions.md
@../PanoramicData.Skills/.github/skills/copilot-instructions.md
