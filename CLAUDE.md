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

Never commit credentials or print tokens. Do not remove assertions, suppress findings,
or weaken coverage thresholds to make checks pass. Live integration tests require Cisco
IQ credentials and must remain separate from unit coverage runs. Do not publish packages
without user authorization.

## Repository conventions

@.github/copilot-instructions.md
@../PanoramicData.Skills/.github/skills/copilot-instructions.md
