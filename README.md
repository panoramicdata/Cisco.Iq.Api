# Cisco.Iq.Api

[![Nuget](https://img.shields.io/nuget/v/Cisco.Iq.Api)](https://www.nuget.org/packages/Cisco.Iq.Api/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/e07981fba4b949c68a2dc6420f21234d)](https://app.codacy.com/gh/panoramicdata/Cisco.Iq.Api/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

A .NET library for the **Cisco IQ** Assets and Assessments REST APIs — asset inventory,
contracts and coverage, hardware and software lifecycle milestones, security advisory (PSIRT)
exposure, and field notice impact.

See the [Cisco IQ FAQ](https://iq.cisco.com/docs/faq.html) for Cisco's platform guidance.

> ### ⚠️ The Cisco IQ API is in beta
>
> Cisco classifies the Cisco IQ APIs as **beta (public preview)** and states:
>
> > API behavior (including endpoint paths, request and response schemas, authentication,
> > pagination, error handling, and other semantics) may change between releases without
> > maintaining backward compatibility. **Do not build production integrations against the
> > beta APIs.**
>
> "Beta" describes the API's maturity, not its availability — it is open to anyone with a
> Cisco IQ account. But there is no backward-compatibility guarantee, and the base path is
> `v0`. This library tracks that API, so **it inherits the same caveat**: expect breaking
> changes, and pin your package version.
>
> This warning is Cisco's position on their API, not a statement about the quality of this
> library.

## Relationship to `Cisco.Api`

[`Cisco.Api`](https://github.com/panoramicdata/Cisco.Api) covers the Cisco Support APIs — EoX,
PSIRT, PSS, Product Information, PX Cloud, Smart Accounts and Umbrella — which authenticate
with OAuth2 client credentials from the [Cisco API Console](https://apiconsole.cisco.com/).

Cisco IQ is a different product with a different host, a different credential type and a
different authentication flow, so it lives in its own package. The two are independent; use
either or both.

## Installation

```bash
dotnet add package Cisco.Iq.Api
```

## Prerequisites

- A **Cisco IQ account** with permission to view the data you want to read
- A long-lived **Personal Access Token (PAT)** or **Service Account Token (SAT)**, generated in
  the Cisco IQ UI
- Your **Account ID** and **Data Storage Region**, from *Home > System Settings > Account Details*

### Generating a Personal Access Token

1. Log in to [Cisco IQ](https://iq.cisco.com/)
2. Click your name (top right) > **User Settings**
3. Click **Generate Token**, give it a name, and click **Generate Token**
4. **Copy it immediately** — Cisco never shows the value again

### Generating a Service Account Token

For unattended automation, prefer a service account. Note that SATs are available only to
**customer-type accounts with the Account Administrator role** — partner-type accounts and
partner roles must use a PAT.

1. Log in to Cisco IQ as an Administrator
2. **Home > System Settings > Identity and Access > Add User**
3. Select the **Service Account** user type, name it, and assign a role
   (**Administrator**, or **Viewer** plus one or more resource groups)
4. Click **Save**, then copy the generated token — again, shown only once

A service account supports up to five concurrent tokens, which is what makes zero-downtime
rotation possible.

## Usage

```csharp
using Cisco.Iq.Api;

CancellationToken cancellationToken = default;

using var client = new CiscoIqClient(new CiscoIqClientOptions
{
    Token = "<your-PAT-or-SAT>",
    AccountRegion = CiscoIqAccountRegion.Emea,
    AccountId = "<your-account-id>",  // required for a PAT, optional for a SAT
    UserAgent = "MyInventoryTool/1.0 MyCompany" // optional override
});

// Assets covered by a contract, most recently seen first
var page = await client.Assets.GetAssetsAsync(new GetAssetsRequest
{
    Filter = new AssetFilter
    {
        CoverageStatus = ["COVERED"],
        Sort = "lastSignalDate",
        Order = CiscoIqSortOrder.Descending,
        Max = 200
    }
}, cancellationToken);

foreach (var asset in page.Content.Items)
{
    Console.WriteLine($"{asset.SerialNumber} {asset.ProductId} {asset.HardwareLastDateOfSupport:d}");
}

// Every asset affected by a critical advisory, paging handled for you
await foreach (var affected in client.Assessments
    .GetAffectedAssetsForSecurityAdvisoryAllAsync(
        new GetAffectedAssetsForSecurityAdvisoryRequest { PsirtId = 82456 }, cancellationToken))
{
    Console.WriteLine(affected.Hostname);
}
```

The library handles the two-stage token exchange, the mandatory `account_region` cookie, access
token caching and renewal, `Link`-header pagination, and rate-limit backoff.

Every collection has a `Get…Async` method returning a `CiscoIqPage<T>` and a
`Get…AllAsync` companion returning `IAsyncEnumerable<T>`. The companions follow the
server's next Link until it is absent; `Meta.Count == null` means the total is unknown.
Filter arrays become repeated query parameters, and date filters use `DateTimeOffset?`.

`UserAgent` overrides the header on both token exchanges and product requests. When it
is null or blank, the default is `Cisco.Iq.Api/<assembly-version>`. A User-Agent is
needed to avoid the CloudFront HTML 403 observed during live verification.

`LastRateLimitStatus` exposes the principal and account second/day limit, remaining and
reset values. Requests retry 429 after the maximum advertised reset, and 502 with
bounded exponential backoff and jitter. `MaxAttemptCount` includes the initial attempt;
the default is three. Disable 429 retries with `RetryRateLimitedRequests = false`.
The HTTP timeout defaults to 100 seconds; raise it if you intend to wait for longer windows.

API errors throw `CiscoIqApiException`, with narrower authentication (401), authorization
(403), not-found (404) and rate-limit (429) types. `BodyTrackingId` and `HeaderTrackingId`
preserve both support identifiers; `TrackingId` prefers the body identifier.

## Development and verification

The solution targets .NET 10, uses central package management and xUnit v3 on
Microsoft.Testing.Platform. Builds treat compiler and MSBuild warnings as errors.

Store integration credentials outside the repository:

```powershell
dotnet user-secrets set "CiscoIq:Token" "<PAT-or-SAT>" --project Cisco.Iq.Api.Test
dotnet user-secrets set "CiscoIq:AccountId" "<account-id>" --project Cisco.Iq.Api.Test
dotnet user-secrets set "CiscoIq:AccountRegion" "EMEA" --project Cisco.Iq.Api.Test
```

Configuration loads user secrets then environment variables (`CiscoIq__Token`,
`CiscoIq__AccountId`, `CiscoIq__AccountRegion`). Without a token, integration tests skip.
The integration smoke test exchanges a token and reads at most one asset from production.
The shared test assembly explicitly sets `failSkips: false` for that credential-free
integration behavior. CI overrides this with `--fail-skips on` in the unit-only coverage run,
so an accidentally skipped unit test still fails the build. Both CI coverage runs use
unit tests only. Pushes to main and release tags also run the live integration test with
`--fail-skips on`, using `CISCO_IQ_TOKEN`, `CISCO_IQ_ACCOUNT_ID` and
`CISCO_IQ_ACCOUNT_REGION` repository secrets. Those credentials are scoped to that step
and are not supplied to pull request builds.

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build
dotnet test --configuration Release --no-build --filter "Category!=Integration" --coverage --coverage-output-format cobertura --coverage-settings coverage.config --coverage-output standard.cobertura.xml --results-directory artifacts/coverage
dotnet test --configuration Release --no-build --filter "Category!=Integration" --coverage --coverage-output-format cobertura --coverage-settings coverage.settings.xml --coverage-output extended.cobertura.xml --results-directory artifacts/coverage
pwsh -File tools/Assert-Coverage.ps1 -Path artifacts/coverage/standard.cobertura.xml -MinimumBranchCoverage 100
pwsh -File tools/Assert-Coverage.ps1 -Path artifacts/coverage/extended.cobertura.xml -MinimumBranchCoverage 100
```

`coverage.config` follows Oscar's generated-code attribute exclusions. The extended
configuration also measures handwritten async method bodies and lambdas, while excluding
generated Refit, logging and regular expression implementations. CI requires 100% line
and branch coverage in both reports.

Fixtures cover all seven models, nullable fields and sparse selection. Sanitized live
fixtures and their provenance are described in [Fixtures/README.md](Cisco.Iq.Api.Test/Fixtures/README.md).

For the first release, version.json is set to `0.1`. Once approved, run `./Publish.ps1`
from a clean main branch synchronized with origin/main. It resolves the package version
using Nerdbank.GitVersioning's MSBuild target, pushes the version tag and checks the
release run. Tagged CI uses NuGet Trusted Publishing; no publication is performed by
ordinary builds or pull requests.

## Documentation

Reference notes on the underlying API — authentication, collection conventions, every
operation and every response schema — are in [documentation/cisco-iq/](documentation/cisco-iq/).

Cisco's own documentation is at <https://iq.cisco.com/api/v1/apiregistry/docs/intro>
(Cisco IQ login required).

## Licence

MIT — see [LICENSE](LICENSE).

Use of the Cisco IQ API itself is governed by the
[Cisco API License](https://developer.cisco.com/site/license/cisco-api-license/). This library
is an independent client and is not affiliated with or endorsed by Cisco Systems, Inc.

Each operation accepts a typed request object and an explicit `CancellationToken`. Requests implement `IRequest<T>` for their payload type. Page and item calls return `IResponse<T>` with `Content` and `StatusCode`; paging helpers return `IAsyncEnumerable<T>`. Add new parameters to the request object without changing the operation signature.
