# Cisco.Iq.Api — design

- **Date:** 2026-09-16
- **Package:** `Cisco.Iq.Api`
- **Repository:** <https://github.com/panoramicdata/Cisco.Iq.Api>
- **Background reference:** [documentation/cisco-iq/](../../../documentation/cisco-iq/)

## Goal

A standalone MIT-licensed .NET client covering the Cisco IQ REST API in full — all 16
operations of release `2026-07-24`, API version `0.1.0`.

## Why a separate package rather than part of `Cisco.Api`

`Cisco.Api` covers the Cisco Support APIs. Every one of them — EoX, PSIRT, PSS, Product
Information, PX Cloud, Enterprise Agreement, Smart Accounts, Umbrella — authenticates with
OAuth2 client credentials issued from the Cisco API Console, driven by
`CiscoClientOptions.ClientId` / `.ClientSecret`.

Cisco IQ shares none of that:

| | `Cisco.Api`'s APIs | Cisco IQ |
| --- | --- | --- |
| Product | Cisco Support (SNTC/PSS) | Cisco IQ |
| Credential | client id + secret, from apiconsole.cisco.com | PAT or SAT, from the Cisco IQ UI |
| Token flow | OAuth2 client credentials | custom exchange at `/cxp-iam/api/v1/auth/issueToken` |
| Host | `apix.cisco.com`, `api.cisco.com`, `swapi.cisco.com` | `iq.cisco.com` |
| Per-request extras | none | mandatory `account_region` **cookie** |
| Data residency | none | `US` / `EMEA` / `APJC` |
| Stability | stable | **beta, `v0`, no compatibility guarantee** |

`CiscoClient`'s constructor throws unless `ClientId` and `ClientSecret` are set and eagerly
builds seven `HttpClient`s. Merging Cisco IQ in would force every Cisco IQ caller to hold
SNTC/PSS credentials they may not have, and vice versa.

The decisive argument is the last row. `Cisco.Api` is a stable package on version 3.x. Cisco
reserves the right to break the IQ API without notice, and a client that tracks it will need
breaking releases to match. Binding that churn to `Cisco.Api`'s version number would force
either misleading major bumps on stable consumers or a stale IQ client. Separate packages let
each version honestly.

The two packages are independent — no project reference in either direction, no shared
abstractions. Consumers take one, the other, or both.

## Package identity

Same family branding as `Cisco.Api`: the same `Icon.png` / `Icon.ico`, `Panoramic Data Limited`
authorship, MIT licence, central package management, Nerdbank.GitVersioning, SourceLink and
symbol packages, and the same CI and trusted-publishing workflow. Versioning starts at `1.0`.

## Repository layout

```
Cisco.Iq.Api.slnx
Directory.Build.props          # shared: nullable, TreatWarningsAsErrors, authorship
Directory.Packages.props       # central package versions
global.json                    # .NET 10, Microsoft.Testing.Platform
version.json                   # Nerdbank.GitVersioning, 1.0
README.md                      # the ONLY place the beta warning appears
SECURITY.md
LICENSE                        # MIT
.github/workflows/ci.yml       # build, pack, publish on tag
.github/workflows/codeql-analysis.yml
documentation/cisco-iq/        # reference notes on the API itself
docs/superpowers/specs/        # this document
Cisco.Iq.Api/
Cisco.Iq.Api.Test/
```

## Public surface

```csharp
namespace Cisco.Iq.Api;

public sealed class CiscoIqClient : IDisposable
{
    public CiscoIqClient(CiscoIqClientOptions options, ILogger? logger = null);

    public IAssets Assets { get; }
    public IAssessments Assessments { get; }

    /// Rate-limit headers from the most recent response, or null if none seen yet.
    public CiscoIqRateLimitStatus? LastRateLimitStatus { get; }
}

public class CiscoIqClientOptions
{
    /// The long-lived Personal Access Token or Service Account Token.
    public required string Token { get; set; }

    /// The account's Data Storage Region.
    public required CiscoIqAccountRegion AccountRegion { get; set; }

    /// Required for a PAT. Optional for a SAT; if set, must match the service account's account.
    public string? AccountId { get; set; }

    public int HttpClientTimeoutSeconds { get; set; } = 100;
    public TimeSpan TokenRefreshMargin { get; set; } = TimeSpan.FromSeconds(60);
    public bool RetryRateLimitedRequests { get; set; } = true;
    public int MaxAttemptCount { get; set; } = 3;
    public string? UserAgent { get; set; }
}

public enum CiscoIqAccountRegion { Us, Emea, Apjc }
public enum CiscoIqSortOrder { Ascending, Descending }
```

`CiscoIqAccountRegion` is an enum because the spec declares exactly three values and getting it
wrong is a routing failure, not a data variation. It serializes to the uppercase wire values
`US`, `EMEA`, `APJC`. `CiscoIqSortOrder` likewise maps to `ASC` / `DESC`.

Two Refit interfaces, matching the two OpenAPI definitions rather than inventing a grouping:
`IAssets` (6 operations) and `IAssessments` (10).

## Authentication

`CiscoIqAuthenticationHandler : DelegatingHandler`, first in the pipeline.

1. **Exchange.** `POST https://iq.cisco.com/cxp-iam/api/v1/auth/issueToken`
   - `Authorization: Basic <raw PAT or SAT>` — **not** Base64 of `user:password`.
     `new AuthenticationHeaderValue("Basic", options.Token)` is correct precisely because it
     performs no encoding.
   - `Cookie: account_region=<REGION>` — required on the exchange as well as on product calls.
   - Body `{"accountId": "..."}` when `AccountId` is set, otherwise `{}`.
2. **Cache** the `accessToken`, expiring at
   `DateTimeOffset.UtcNow + expiresInSeconds - TokenRefreshMargin`. `expiresInSeconds` is read
   from the response, never assumed to be 3600.
3. **Serialize refreshes** behind a `SemaphoreSlim(1,1)` with double-checked validity, so N
   concurrent requests trigger one exchange rather than N. The user rate limit is 10 req/s — a
   stampede against the exchange endpoint would itself trip it.
4. **Stamp every product request** with `Authorization: Bearer <accessToken>`,
   `Cookie: account_region=<REGION>` and `Accept: application/json`.
5. **Re-exchange once on 401**, then retry the request once. A second 401 propagates — the token
   is revoked or the identity has lost access, and retrying will not help.

The exchange uses its own bare `HttpClient` with no handler pipeline, so a failing exchange can
never recurse into itself.

### The cookie

Refit has no cookie parameter, so `account_region` is set as a `Cookie` request header by the
handler. **`HttpClientHandler.UseCookies` must be `false`** — with the default `true`, the
handler owns the cookie container and discards a manually set `Cookie` header. This is the most
likely cause of an otherwise-inexplicable 400 during implementation, and needs a test asserting
the header survives to the wire.

## Rate limiting and retries

`CiscoIqRateLimitHandler : DelegatingHandler`, after the auth handler.

Published limits: 10/s and 5,000/day per user; 25/s and 25,000/day per account. Both must be
satisfied.

- **429:** read the exhausted window's `x-…-ratelimit-reset` (seconds), wait, retry, bounded by
  `MaxAttemptCount`. A 429 does not reliably carry all twelve headers — only the exceeded
  window's are guaranteed — so take the maximum `reset` of whichever are present, falling back
  to a fixed delay if none are.
- **502:** bounded exponential backoff with jitter.
- **Nothing else retries.** 400, 401 (beyond the single re-exchange above), 403, 404 and 406 are
  caller errors a retry cannot fix.

`CiscoIqRateLimitStatus` exposes the twelve headers from the last response as four
`limit`/`remaining`/`reset` triples, so callers can pace bulk work themselves.

## Models

Namespace `Cisco.Iq.Api.Data`. Seven response types — `Asset`, `AffectedAsset`,
`AssetLifecycle`, `AssetRelationship`, `Contract`, `SecurityAdvisory`, `FieldNotice` — plus
`CollectionMeta` and `ErrorBody`. Singularized from the spec's plural schema names, which name
the collection rather than the item.

Newtonsoft attributes throughout (`Refit.Newtonsoft.Json`), matching `Cisco.Api`'s serializer
choice so the two packages behave consistently for anyone using both.

**Every property is nullable**, including those the spec marks required. Two independent
reasons: the OpenAPI definitions type most fields as `["string","null"]`, and the `fields`
sparse-selection parameter means any property can be absent from any response.

### Dates

All date fields are Unix epoch **milliseconds** as `int64`, exposed as `DateTimeOffset?` via a
`UnixMillisecondsConverter : JsonConverter<DateTimeOffset?>` using
`DateTimeOffset.FromUnixTimeMilliseconds`. The raw `long` is not also exposed — the conversion
is exact and reversible, so nothing is lost.

### Types kept as declared

- `affectedAssetsCount` / `potentiallyAffectedAssetsCount` are `string?` on `SecurityAdvisory`
  but `int?` / `int` on `FieldNotice`. Not a spec error — Cisco's own example returns them
  quoted. Modelled as declared; coercing would break on the first non-numeric value.
- Status-like fields (`impact`, `coverageStatus`, `vulnerabilityStatus`, milestone names) stay
  `string?`. No enum is declared for any of them, observed values are inconsistently cased
  (`COVERED`, `Critical`, `Last Date of Support`), and a beta API is exactly where an
  unanticipated value shows up.

## Collections and paging

```csharp
public class CiscoIqPage<T>
{
    public List<T> Items { get; set; }
    public CollectionMeta Meta { get; set; }
}
```

Refit methods return `Task<ApiResponse<CiscoIqPage<T>>>` internally so `Link` and
`X-Total-Count` are reachable, with the ergonomic surface wrapping that.

`CollectionMeta.Count` is `long?`. **Null means "total unknown", not zero** — Cisco is explicit
about this, and it must never be used to decide whether more pages exist.

Each list operation gets an `IAsyncEnumerable<T>` companion that **follows the `Link` rel="next"
header** rather than incrementing `offset` — Cisco asks for this directly, and the header's
absence is the only reliable end-of-collection signal. `Link` URLs are relative and resolve
against the request URI.

## Filters

`GET /assets` takes 51 query parameters. A Refit method with 51 parameters is unusable, and
every filter Cisco adds later would be a source-breaking change.

**Decision: filter request objects.** A `CiscoIqFilter` base carries the shared `Max`, `Offset`,
`Sort`, `Order` and `Fields`; `AssetFilter`, `ContractFilter`, `SecurityAdvisoryFilter` and
`FieldNoticeFilter` add their own. The affected-asset and relationship collections declare no
filters of their own and take `CiscoIqFilter` directly.

Applied uniformly to every collection endpoint, not just `/assets` — a client with two idioms
for the same job is worse than one verbose object.

Serialization needs a custom `IUrlParameterFormatter` handling:

- **Array filters as repeated parameters** — `?productId=a&productId=b`, per `explode: true`.
  Not comma-joined.
- **`DateTimeOffset?` date filters back to epoch milliseconds.**
- **Omitting nulls entirely**, so an unset filter contributes no query parameter.
- **`Order`** as uppercase `ASC` / `DESC`.

`Sort` stays a `string`, not an enum. The OpenAPI definitions declare no allowed values, and the
documented list exists only for `/assets`.

## Errors

`CiscoIqApiException : Exception` carrying `StatusCode`, the `ErrorBody` message, and
`TrackingId`. The tracking id must be read from **both** the body's `trackingId` and the
`TrackingID` response header — they appear independently, and they are what Cisco TAC needs.

Subtypes where behaviour differs and callers will reasonably catch narrowly:
`CiscoIqAuthenticationException` (401), `CiscoIqAuthorizationException` (403),
`CiscoIqNotFoundException` (404), `CiscoIqRateLimitException` (429, carrying the reset seconds).

## Beta handling

Cisco states: *"Do not build production integrations against the beta APIs."* The base path is
`v0` and Cisco reserves the right to break it without notice.

**Decision: ship it, with the beta warning in `README.md` only.** No `[Experimental]` attribute,
no XML doc remarks on public types, no `PackageReleaseNotes` line. The warning is stated once,
prominently, where anyone evaluating the package will read it — including on nuget.org, since
the README is packed.

## Testing

- **Unit tests, no network.** A stubbed `HttpMessageHandler` covers what is most likely to
  break: `Basic <raw-token>` is not Base64-encoded; `account_region` survives to the wire as a
  `Cookie` header; token caching does not re-exchange within the validity window; concurrent
  requests trigger exactly one exchange; a 401 triggers exactly one re-exchange and retry; a 429
  waits the advertised `reset`; array filters serialize as repeated parameters; epoch-millisecond
  round-tripping is exact; a `meta.count: null` response is not treated as empty.
- **Deserialization tests** against captured sample payloads for all seven models, including a
  sparse `fields` response.
- **Integration tests**, skipped unless credentials are configured.

### Credentials in tests

**User secrets**, not `appsettings.json` — `UserSecretsId` is set on the test project, so
credentials live outside the repository entirely and cannot be committed by accident.

```
CiscoIq:Token          the PAT or SAT
CiscoIq:AccountId      from Home > System Settings > Account Details
CiscoIq:AccountRegion  US | EMEA | APJC
```

Configuration is built with `AddUserSecrets<T>()` then `AddEnvironmentVariables()`, so CI can
supply the same values as `CiscoIq__Token` etc. Integration tests skip — not fail — when
`CiscoIq:Token` is absent, so a fresh clone builds and tests green with no setup.

`appsettings.json` remains gitignored as a second line of defence.

## Out of scope

Write operations, webhooks and a sandbox — none exist in the API. No caching beyond the access
token. No CMDB/ITSM mapping helpers. No dependency on, or shared code with, `Cisco.Api`.

## Risks

| Risk | Mitigation |
| --- | --- |
| Cisco breaks the API without notice (beta, `v0`) | README warning; independent versioning; integration tests catch it at the next run |
| `UseCookies` default silently strips `account_region` | Explicit test asserting the header reaches the wire |
| A helper Base64-encodes the `Basic <PAT>` value | Explicit test asserting the raw token is sent |
| Paging helpers trip the 10 req/s user limit | Rate-limit handler honours `reset`; paging is sequential, not parallel |
| No sandbox — integration tests hit production | Read-only API; small `max` values; opt-in via user secrets |
| A token is committed | User secrets by default; `appsettings.json` gitignored; no token ever logged |
