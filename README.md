# Cisco.Iq.Api

[![Nuget](https://img.shields.io/nuget/v/Cisco.Iq.Api)](https://www.nuget.org/packages/Cisco.Iq.Api/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/e07981fba4b949c68a2dc6420f21234d)](https://app.codacy.com/gh/panoramicdata/Cisco.Iq.Api/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

A .NET library for the **Cisco IQ** REST APIs: asset inventory, contracts and coverage,
hardware and software end-of-life bulletins, security advisories (PSIRT), field notices and
assessment findings.

The official Cisco IQ API documentation, including the beta-to-GA API mapping guide, is at
<https://iq.cisco.com/apidocs/introduction>. See also the
[Cisco IQ FAQ](https://iq.cisco.com/docs/faq.html) for Cisco's platform guidance.

> ### The Cisco IQ beta APIs stop working on 30 November 2026
>
> Cisco has made the Cisco IQ APIs generally available (GA). The beta (`v0`) APIs keep working
> until **30 November 2026**, after which they stop functioning.
>
> This package now contains a GA client, `Cisco.Iq.Api.V1.CiscoIqV1Client`. The beta client,
> `CiscoIqClient`, is marked obsolete and will be removed after the sunset date. Move to the GA
> client before then: see [Moving from the beta client](#moving-from-the-beta-client).

## What is covered

| API | Namespace | State |
| --- | --- | --- |
| Asset (physical and virtual assets, coverage, data sources, licensing) | `Cisco.Iq.Api.V1.Assets` | Read operations |
| Assessment (assessments, findings, insights, recommendations) | `Cisco.Iq.Api.V1.Assessments` | Read operations |
| Product (products, security advisories, field notices, end-of-life bulletins) | `Cisco.Iq.Api.V1.Products` | Read operations |
| Asset writes (`PATCH` and `PUT`) | | Not yet generated |
| IAM (accounts, users, API keys, roles) | | Not yet generated |
| System (resource groups, preferences, query, bulk) | | Not yet generated |

The client is generated from the OpenAPI files Cisco publishes, which are kept in
[docs/ga-spec/](docs/ga-spec/). It has not yet been verified against a live Cisco IQ account.

## Relationship to `Cisco.Api`

[`Cisco.Api`](https://github.com/panoramicdata/Cisco.Api) covers the Cisco Support APIs (EoX,
PSIRT, PSS, Product Information, PX Cloud, Smart Accounts and Umbrella), which authenticate
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
- A long-lived **API key**. It acts as its owner, with the owner's current permissions, so the
  key can read only what its owner can read
- The **region** that holds your account: US, EMEA or APJC

Cisco's [authentication documentation](https://iq.cisco.com/apidocs/introduction) describes how
to create a key. A key is returned once, when it is created, so store it in a secret store and
never in source control. Keys have a maximum lifetime and are never refreshed: create a
replacement, switch over, then revoke the old one.

## Usage

```csharp
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Products;

CancellationToken cancellationToken = default;

using var client = new CiscoIqV1Client(new CiscoIqV1ClientOptions
{
    ApiKey = "<your-api-key>",
    Region = CiscoIqV1Region.Emea
});

// One page of managed assets, ordered by model, with the total count
using var page = await client.Assets.ListAssetPhysicalAssetsAsync(new ListAssetPhysicalAssetsRequest
{
    Filter = "isManaged == true",
    OrderBy = "model",
    Max = 100,
    Count = true
}, cancellationToken: cancellationToken);

Console.WriteLine($"{page.Content!.Count} managed assets");
foreach (var asset in page.Content.Items!)
{
    Console.WriteLine($"{asset.Serial} {asset.Model}");
}

// Every critical security advisory, paging handled for you
await foreach (var advisory in client.Products.ListProductSecurityAdvisoriesAllAsync(
    new ListProductSecurityAdvisoriesRequest { Filter = "severity == \"CRITICAL\"" }, cancellationToken))
{
    Console.WriteLine($"{advisory.AdvisoryId} {advisory.Title}");
}
```

### Collections

Every collection has a `List...Async` method that returns one page, and a `List...AllAsync`
companion that returns `IAsyncEnumerable<T>` and follows the cursor in the `Link` header until
there is no next page. The cursor is opaque: the client passes it back unchanged.

- `Max` sets the page size, `OrderBy` the sort, `Select` a sparse set of fields, and `Filter`
  one filter expression. A filter is applied before paging.
- `Count = true` adds the total to the first page. It is not sent while enumerating.
- Enum-like values are string-backed structs such as `AssetOrigin.Cisco`. A value Cisco adds
  later is kept as its string, instead of failing to parse.
- Relations are reference strings such as `/product/v1/products/<id>`.
- Single-resource reads accept `ifNoneMatch` and `ifModifiedSince` for conditional requests.

A query string over **2,048 bytes** after encoding is refused by Cisco's edge before the API
sees it. The client throws `CiscoIqV1QueryTooLongException` rather than send it. Use the System
API's query endpoint for long filters once it is available in this package.

### Regions and hosts

By default the client calls `https://iq.cisco.com` and sends `X-IQ-Region` with the region you
set. Cisco's documentation also names regional hosts (`us-west-2.iq.cisco.com`,
`eu-central-1.iq.cisco.com`, `ap-southeast-2.iq.cisco.com`). To use one, set `BaseAddress`. The
OpenAPI files name different regional hosts, so confirm the host for your account. A key for
the wrong region is rejected with 401.

### Errors, retries and rate limits

API errors throw `CiscoIqV1ApiException`, with narrower types for 401
(`CiscoIqV1AuthenticationException`), 403 (`CiscoIqV1AuthorizationException`), 404
(`CiscoIqV1NotFoundException`) and 429 (`CiscoIqV1RateLimitException`). Cisco returns
`application/problem+json`; the parsed document is on `Problem`, and `HasErrorCode("...")`
checks its error codes. Branch on error codes, not on the status: the set of codes is open.
`Traceparent` carries the trace identifier to quote in a Cisco support case.

429 responses are retried after `Retry-After`, and 502 and 503 responses to reads are retried
with bounded backoff. `MaxAttemptCount` includes the first attempt (default three), and
`RetryRateLimitedRequests = false` turns 429 retries off. Cisco does not publish numeric rate
limits; `LastRateLimit` exposes the raw `RateLimit` and `RateLimit-Policy` header values from
the latest response.

The API key is never written to logs or exception messages. Redirects are not followed, so the
key cannot be sent to another host.

## Moving from the beta client

GA is a new contract, not a version bump. Every path, parameter and schema differs.

| | Beta client (`CiscoIqClient`) | GA client (`CiscoIqV1Client`) |
| --- | --- | --- |
| Credential | PAT or SAT, exchanged for a short-lived token | API key, used directly as the bearer token |
| Region | `account_region` cookie and account ID | Region header or regional host |
| Paging | `offset` and `max`, `meta` in the body | `max` and an opaque cursor in the `Link` header |
| Filtering | Typed filter objects and repeated parameters | One `Filter` expression string |
| Errors | `CiscoIqApiException` | `CiscoIqV1ApiException` with an RFC 9457 `Problem` |

Cisco's mapping guide, linked from the announcement email and the documentation site, is the
authoritative source for how beta operations map to GA. From the OpenAPI files alone, these
pairings look right; treat them as a guide and check them against Cisco's mapping guide:

| Beta operation | GA operation | Confidence |
| --- | --- | --- |
| Assets list and get | `Assets.ListAssetPhysicalAssetsAsync`, `GetAssetPhysicalAssetAsync` | High |
| Contracts list and get | `Assets.ListAssetCoveragesAsync`, `GetAssetCoverageAsync` | High |
| Security advisories list and get | `Products.ListProductSecurityAdvisoriesAsync`, `GetProductSecurityAdvisoryAsync` | High |
| Field notices list and get | `Products.ListProductFieldNoticesAsync`, `GetProductFieldNoticeAsync` | High |
| Asset lifecycle | End-of-life bulletins and asset fields | Low, inferred |
| Asset relationships | `Assets.ListAssetChildrenForPhysicalAssetAsync` and the `Parent` property | Low, inferred |
| Advisories or field notices for an asset; affected assets | None documented | |

For the last row, the closest routes are the Assessment API's findings filtered by asset, or
matching an advisory's or field notice's affected products against an asset's `ProductId`.
Neither is documented by Cisco, and findings exist only once an assessment has run, so this
package does not wrap them.

## Development and verification

The solution targets .NET 10, uses central package management and xUnit v3 on
Microsoft.Testing.Platform. Builds treat compiler and MSBuild warnings as errors.

The GA client is generated. After changing a file in `docs/ga-spec/`, run the generator from
the repository root and commit the result; a unit test fails if the committed code is stale.

```powershell
dotnet run --project tools/Cisco.Iq.Api.Generator
dotnet run --project tools/Cisco.Iq.Api.Generator -- --check
```

Run the unit tests and the coverage gates exactly as CI does:

```powershell
dotnet build --configuration Release
dotnet test --configuration Release --no-build --filter "Category!=Integration" --fail-skips on
dotnet test --configuration Release --no-build --filter "Category!=Integration" --coverage --coverage-output-format cobertura --coverage-settings coverage.config --coverage-output standard.cobertura.xml --results-directory artifacts/coverage
dotnet test --configuration Release --no-build --filter "Category!=Integration" --coverage --coverage-output-format cobertura --coverage-settings coverage.settings.xml --coverage-output extended.cobertura.xml --results-directory artifacts/coverage
pwsh -File tools/Assert-Coverage.ps1 -Path artifacts/coverage/standard.cobertura.xml -MinimumBranchCoverage 100
pwsh -File tools/Assert-Coverage.ps1 -Path artifacts/coverage/extended.cobertura.xml -MinimumBranchCoverage 100
```

CI requires 100% line and branch coverage of the hand-written code in both reports. Generated
code under `Cisco.Iq.Api/V1/Generated` is excluded from both: it is covered instead by the
generator drift test and by a test that every generated operation requests its documented path.

Integration tests exercise the beta client against a live account. Store credentials outside
the repository:

```powershell
dotnet user-secrets set "CiscoIq:Token" "<PAT-or-SAT>" --project Cisco.Iq.Api.Test
dotnet user-secrets set "CiscoIq:AccountId" "<account-id>" --project Cisco.Iq.Api.Test
dotnet user-secrets set "CiscoIq:AccountRegion" "EMEA" --project Cisco.Iq.Api.Test
```

Configuration loads user secrets then environment variables (`CiscoIq__Token`,
`CiscoIq__AccountId`, `CiscoIq__AccountRegion`). Without a token, integration tests skip, and
the shared test assembly sets `failSkips: true`, so a full test run fails. Use
`--filter "Category!=Integration"` for credential-free unit runs. Integration tests never run on
push or pull request. The live check is the manually triggered `Live Cisco IQ integration`
workflow. Cisco IQ has returned HTTP 403 for GitHub-hosted runners while the same credentials
pass locally, so it is not a release gate.

Version numbers come from `version.json` and Nerdbank.GitVersioning. Run `./Publish.ps1` from a
clean main branch synchronised with origin/main: it resolves the version, pushes the tag and
checks the release run. Tagged CI uses NuGet Trusted Publishing; ordinary builds and pull
requests publish nothing.

## Legacy beta client

> Obsolete. The beta APIs stop working on 30 November 2026.

`CiscoIqClient` targets the beta `v0` API. It signs in with a Personal Access Token or a
Service Account Token, which it exchanges for a short-lived access token, and it sends the
mandatory `account_region` cookie.

```csharp
using Cisco.Iq.Api;

using var client = new CiscoIqClient(new CiscoIqClientOptions
{
    Token = "<your-PAT-or-SAT>",
    AccountRegion = CiscoIqAccountRegion.Emea,
    AccountId = "<your-account-id>"  // required for a PAT, optional for a SAT
});

var page = await client.Assets.GetAssetsAsync(new GetAssetsRequest
{
    Filter = new AssetFilter { CoverageStatus = ["COVERED"], Max = 200 }
}, CancellationToken.None);
```

Every beta collection has a `Get...Async` method returning a `CiscoIqPage<T>` and a
`Get...AllAsync` companion returning `IAsyncEnumerable<T>`. Errors throw `CiscoIqApiException`
and its narrower types. Each operation accepts a typed request object and an explicit
`CancellationToken`.

## Documentation

Reference notes on the underlying beta API (authentication, collection conventions, every
operation and every response schema) are in [documentation/cisco-iq/](documentation/cisco-iq/).
They describe the beta contract and are kept for the migration period.

Cisco's own documentation for the GA API is at <https://iq.cisco.com/apidocs/introduction>.

### Cisco DevNet sandboxes

The [Cisco DevNet Sandbox catalog](https://devnetsandbox.cisco.com/DevNet) provides
environments for developing and testing Cisco product integrations. Always-On entries can often
be accessed without a reservation; other entries require launching or reserving an environment.
Consult each entry's current instructions for availability and access.

Sandbox access alone does not establish a Cisco IQ data connector. Cisco's
[SD-WAN telemetry prerequisites](https://iq.cisco.com/docs/saas/saas-getting-started.html#prerequisites-for-data-collection-for-cisco-catalyst-sd-wan-telemetry)
require an associated Smart Account and unique organization name, Smart Account or Virtual
Account administrator access, Cisco IQ Account Administrator access, and enabled telemetry
collection. Obtain a suitably authorized sandbox organization from Cisco before using it to
test ingestion into IQ. Read credentials from the catalog rather than storing them in this
repository.

## Licence

MIT: see [LICENSE](LICENSE).

Use of the Cisco IQ API itself is governed by the
[Cisco API License](https://developer.cisco.com/site/license/cisco-api-license/). This library
is an independent client and is not affiliated with or endorsed by Cisco Systems, Inc.
