# Cisco.Iq.Api

[![Nuget](https://img.shields.io/nuget/v/Cisco.Iq.Api)](https://www.nuget.org/packages/Cisco.Iq.Api/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Codacy Badge](https://app.codacy.com/project/badge/Grade/e07981fba4b949c68a2dc6420f21234d)](https://app.codacy.com/gh/panoramicdata/Cisco.Iq.Api/dashboard?utm_source=gh&utm_medium=referral&utm_content=&utm_campaign=Badge_grade)

> **Need help with APIs or .NET?** Panoramic Data are experts in APIs and .NET, and we can help with your development. [Contact us](https://www.panoramicdata.com/contact-us) or see our [software services](https://www.panoramicdata.com/services/software).

A .NET library for the **Cisco IQ** REST APIs: asset inventory, contracts and coverage,
hardware and software end-of-life bulletins, security advisories (PSIRT), field notices and
assessment findings.

Supports .NET 11.0.

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

Every operation in the five GA OpenAPI files is generated: 213 operations by method and path.

| API | Client property | Namespace | Operations |
| --- | --- | --- | --- |
| Asset (physical and virtual assets, coverage, data sources, licensing) | `Assets` | `Cisco.Iq.Api.V1.Assets` | 28 |
| Assessment (assessments, findings, insights, recommendations) | `Assessments` | `Cisco.Iq.Api.V1.Assessments` | 16 |
| Product (products, security advisories, field notices, end-of-life bulletins) | `Products` | `Cisco.Iq.Api.V1.Products` | 25 |
| IAM (accounts, users, groups, roles, policies, service accounts, API keys) | `Identity` | `Cisco.Iq.Api.V1.Identity` | 100 |
| System (resource groups, preferences, query, bulk, schema discovery) | `Platform` | `Cisco.Iq.Api.V1.Platform` | 44 |

The client is generated from the OpenAPI files Cisco publishes, which are kept in
[docs/ga-spec/](docs/ga-spec/). Every operation is checked against the specification (method, path,
body and headers). **Read operations have also been checked against a live EMEA account** on
10 November 2026: 22 list operations, cursor paging, and the first item of each collection with
its related sub-collections (40 reads), all without errors. Collections that were empty on that
account, and the write, delete and API key operations, have **not** been exercised live: treat
them with care until you have tried them against your own account.

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
API's query endpoint (`Platform.QueryAsync`) for long filters.

### Regions and hosts

By default the client calls `https://iq.cisco.com` and sends `X-IQ-Region` with the region you
set. This was checked against a live EMEA account on 10 November 2026: the global host works,
and so does the documented regional host for that region (`eu-central-1.iq.cisco.com`). The
other documented regional hosts (`us-west-2.iq.cisco.com`, `ap-southeast-2.iq.cisco.com`)
answer 401 to a key from another region, which is the expected rejection. To use a regional
host, set `BaseAddress`.

The hosts that appear in the OpenAPI files (`us-east-1.iq.cisco.com` and
`eu-frankfurt-1.iq.cisco.com`) do not exist in DNS, so do not use them.

### Writes

`PATCH` operations use JSON Merge Patch, in which a member can be left out (keep it), set, or
sent as `null` (clear it). Patch bodies use `CiscoIqV1Optional<T>` for every member, so all three
are possible. Members you do not set are not sent.

```csharp
using Cisco.Iq.Api.V1;
using Cisco.Iq.Api.V1.Assets;

using var updated = await client.Assets.UpdateAssetPhysicalAssetAsync(
    "<asset-id>",
    new UpdatePhysicalAssetRequest
    {
        Tags = new Dictionary<string, string> { ["site"] = "london" },     // set
        Criticality = CiscoIqV1Optional.Null<AssetCriticality>()           // clear
    },
    ifMatch: "<etag-from-the-last-read>");
```

`PUT` and `POST` bodies are plain objects. Read-only properties that Cisco returns, such as `Id`
and `Serial`, are never written back. Use `ifMatch` with the `ETag` from a read to avoid
overwriting a change someone else made; a stale value returns 412.

Some operations are destructive or sensitive: deleting resource groups, users and API keys,
revoking keys, and creating a key, whose token is returned only once. They are exposed exactly as
Cisco defines them and are never exercised against a live account by this repository's tests.

### The System query endpoint

`Platform.QueryAsync` runs a filter, projection and aggregation over one entity type, and pages
through its request and response bodies instead of a `Link` header. `Platform.QueryAllAsync`
follows the cursor for you and does not change your request object. It is also the way to run a
filter too long for a query string.

### Where the specification and the service disagree

Cisco's OpenAPI files are the source of the generated client, with documented exceptions in
[docs/ga-spec/overrides.json](docs/ga-spec/overrides.json), each with its evidence. A test fails
if an exception stops being applied. Found so far:

- `GET /api/assessment/v1/assessments/{id}/supportTiers` is typed as a list of reference strings,
  but returns the entitlement tier resources. Its `Items` is a list of `JToken`.
- The regional hosts in the OpenAPI files (`us-east-1`, `eu-frankfurt-1`) do not exist; the hosts
  in Cisco's documentation do.

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

The solution targets .NET 11.0, uses central package management and xUnit v3 on
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

The GA client has its own read-only live checks (`V1/GaLiveTests`): list operations with a page
size of one, cursor paging, and a walk that reads the first item of each collection and its
relations. They never write, delete or create anything. They need a Cisco IQ API key:

```powershell
dotnet user-secrets set "CiscoIqV1:ApiKey" "<key>" --project Cisco.Iq.Api.Test
dotnet user-secrets set "CiscoIqV1:Region" "EMEA" --project Cisco.Iq.Api.Test
dotnet test --configuration Release --filter "FullyQualifiedName~GaLiveTests"
```

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
