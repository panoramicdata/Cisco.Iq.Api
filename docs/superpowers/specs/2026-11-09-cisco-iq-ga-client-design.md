# Cisco.Iq.Api GA client: design

- **Date:** 2026-11-09
- **Status:** draft, for review
- **Supersedes (in part):** [2026-09-16-cisco-iq-client-design.md](2026-09-16-cisco-iq-client-design.md), which covers the beta `v0` client
- **Source contract:** the five OpenAPI 3.2.0 files in [docs/ga-spec/](../../ga-spec/)
- **Official documentation:** <https://iq.cisco.com/apidocs/introduction>

## Why

Cisco announced on 2026-10-26 that the Cisco IQ APIs are generally available. The beta (`v0`)
APIs stop working on **30 November 2026**. The current client targets only the beta API, so it
stops working on that date.

GA is a new contract, not a version bump of beta. Cisco's changelog (v1.0.0, 2026-10-23) lists
"no breaking changes" because there is no earlier GA contract; every beta path, parameter and
schema differs.

## Decisions already made

| Decision | Choice |
| --- | --- |
| Approach | A new GA client alongside the beta client; the beta client is marked obsolete, then removed after 30 November 2026 |
| Scope | The full GA surface (214 operations across five APIs, including writes, IAM and System) |
| Source of truth | The five OpenAPI files, saved in `docs/ga-spec/`; the client is generated from them |

## What changed from beta

| | Beta `v0` | GA `v1` |
| --- | --- | --- |
| Base URL | `https://iq.cisco.com/ciq-rest/api/v0` | `https://{host}/api/{namespace}/v1`, namespaces `assessment`, `asset`, `iam`, `product`, `system` |
| Credential | PAT or SAT, exchanged at `/cxp-iam/api/v1/auth/issueToken` for a short-lived token | A long-lived API key used directly as the bearer token. No exchange, no refresh |
| Region | `account_region` cookie, `accountId` in the exchange body | Regional host, or `X-IQ-Region: US\|EMEA\|APJC` on `iq.cisco.com`. A wrong-region key returns 401 |
| Paging | `offset` and `max`, `Link` header, `meta` body object | `max` and opaque `after` cursor from the `Link` header; body is `{count?, items}` |
| Filtering | Repeated query parameters | One `filter=` expression (CEL-like), plus `orderby` and `select` |
| Errors | Beta error body | `application/problem+json` (RFC 9457) with `errors[].code` |
| Resources | Flat beta schemas | Resource-oriented: every record has `_type`, `id` and `sys`; relations are reference strings such as `/product/v1/products/<id>` |
| Operations | 16, all GET | 214: Assessment 16, Asset 28, IAM 106, Product 25, System 39 (per Cisco's changelog) |

## Package layout

The beta client stays in `Cisco.Iq.Api`, marked `[Obsolete]` with a message naming the GA
client and the sunset date. The GA client lives in a new namespace in the same package:

```
Cisco.Iq.Api/
  V1/
    CiscoIqV1Client.cs           hand written: construction, pipeline, disposal
    CiscoIqV1ClientOptions.cs    hand written
    CiscoIqV1Region.cs           hand written
    Http/                        hand written: handlers, paging, problem parsing
    Generated/
      Asset/                     generated: IAssetApi (Refit) and models
      Assessment/
      Product/
      Iam/
      System/
tools/Generate-CiscoIqV1/        the generator
Cisco.Iq.Api.Test/V1/
```

One package keeps a single README, version and release for consumers moving from beta to GA.
The beta types stay at their current names, so existing callers compile with obsolete warnings
until they switch.

## Public surface

```csharp
namespace Cisco.Iq.Api.V1;

public sealed class CiscoIqV1Client : IDisposable
{
	public CiscoIqV1Client(CiscoIqV1ClientOptions options, ILogger? logger = null);

	public IAssetApi Asset { get; }
	public IAssessmentApi Assessment { get; }
	public IProductApi Product { get; }
	public IIamApi Iam { get; }
	public ISystemApi System { get; }

	/// RateLimit headers from the most recent response, or null if none seen yet.
	public CiscoIqRateLimitStatus? LastRateLimitStatus { get; }
}

public class CiscoIqV1ClientOptions
{
	public required string ApiKey { get; set; }
	public required CiscoIqV1Region Region { get; set; }

	/// Overrides the host. Use for a regional host, or a test server.
	public Uri? BaseAddress { get; set; }

	public int HttpClientTimeoutSeconds { get; set; } = 100;
	public bool RetryRateLimitedRequests { get; set; } = true;
	public int MaxAttemptCount { get; set; } = 3;
	public string? UserAgent { get; set; }
}

public enum CiscoIqV1Region { Us, Emea, Apjc }
```

Each generated interface is a Refit interface, as in the beta client, one per OpenAPI file.
Every operation takes a `CancellationToken`. List operations come in two forms, matching the
beta convention: a one-page call that returns the items with their paging state, and an
`IAsyncEnumerable<T>` that follows the cursor to the end.

## Authentication

A `DelegatingHandler` stamps `Authorization: Bearer <ApiKey>` and `Accept` on every request. It
does no exchange, no caching and no refresh, because the key is the credential.

- **No retry on 401.** A key does not refresh, so a 401 means the key is wrong, expired,
  revoked, or for another region. The error carries `WWW-Authenticate` and a message naming
  those causes.
- **Keys never appear in logs, exceptions or `ToString()`.** The beta client already redacts
  its token; the same tests are repeated for the key.
- API key lifecycle operations (`POST /api/iam/v1/apiKeys`, revoke) are exposed like any other
  operation. The `token` returned once on creation is a secret and is excluded from logging.

### Host and region

The documentation and the specs disagree on the regional hosts:

| Source | Hosts |
| --- | --- |
| Documentation | `us-west-2.iq.cisco.com`, `eu-central-1.iq.cisco.com`, `ap-southeast-2.iq.cisco.com` |
| OpenAPI `servers` | `iq.cisco.com` (global), `us-east-1.iq.cisco.com`, `eu-frankfurt-1.iq.cisco.com`; no APJC |

Both sources agree on the global host `iq.cisco.com` with an `X-IQ-Region` header. The default
is therefore **`https://iq.cisco.com` plus `X-IQ-Region`**, with `BaseAddress` as an override.
A live call decides whether the regional hosts are needed; see open question 2.

## Paging

- Parse the `Link` header (RFC 5988) for `rel="next"`; the cursor is opaque and is passed back
  unchanged, with `filter`, `orderby` and `select` identical on every page.
- Stop when there is no `next`. Never construct a cursor.
- `count=true` adds `count` to the body and is honoured only on the first page, so it is a
  separate option on the one-page call and is not sent while enumerating.
- `max` defaults to the server default (25). The documented range is 0 to 50000; the client
  does not clamp it, since the real ceiling is unverified.
- The query endpoint (`POST /api/system/v1/query`) pages through its body, with `after` null on
  the last page, so it gets its own enumerator.

## Filtering and long queries

`filter` is passed as a string; there is no expression builder in the first release. A filter
beyond 2,048 bytes after encoding is rejected at the edge with an HTML 403 and no problem
document, so the client throws a specific `CiscoIqQueryTooLongException` before sending when
the encoded query exceeds that limit, and the message points to `POST /api/system/v1/query`.

## Errors

- Parse `application/problem+json` into `CiscoIqProblem` (type, title, status, detail,
  instance, `errors[]` of code, message, description, field).
- Branch on `errors[].code`, not on status. The code list is open: unknown codes are kept as
  strings, never an enum that throws.
- Two rejections carry no problem document and no `traceparent`: the missing-region 400 and the
  oversized-query HTML 403. Both map to typed exceptions with an explanatory message.
- `429 RATE_LIMITED` honours `Retry-After` (seconds or an HTTP date) when
  `RetryRateLimitedRequests` is set, up to `MaxAttemptCount`. No numeric limits are published, so
  none are assumed.
- `traceparent` is sent on every request and the response value is attached to exceptions, to
  make Cisco support cases easier.

## Conditional requests

Single resources carry an `ETag`. `PATCH` and `PUT` support `If-Match`, and reads support
`If-None-Match`; `304` and `412` are results, not failures. The response wrapper exposes `ETag`
and `Last-Modified`.

## Generation

The client is generated from `docs/ga-spec/*.yaml` by `tools/Generate-CiscoIqV1`, and the output
is committed under `Cisco.Iq.Api/V1/Generated/`. Committing the output keeps the build free of a
generation step and makes API changes reviewable as diffs.

Rules the generator follows:

- **Operations are counted by method and path**, not by `operationId`, which repeats in the
  specs (for example `getSystemContactRequest` appears twice). The generator reports the count
  per API and fails if it differs from the expected total.
- Models use Newtonsoft.Json attributes, matching the beta client and the repository's Refit
  convention.
- `anyOf` with `null` becomes a nullable property. Other `anyOf` and `oneOf` cases, including the
  one discriminator in each of the Assessment and System specs, are listed by the generator and
  mapped by hand if it cannot.
- `additionalProperties` becomes a dictionary (`tags` is a writable map on assets).
- Relation fields stay strings. A reference helper parses `/{namespace}/v1/{collection}/{id}`.
- Enums become string-backed types that keep unknown values, because Cisco states that
  `status` and similar values may grow.
- `readOnly` properties are not serialised on write requests.
- Operations tagged `deprecated` or `preview` get `[Obsolete]` or a `[Preview]` attribute. None
  of the current operations carry either tag.
- Lists also accept `text/csv` and xlsx. The first release exposes JSON only.

A drift test regenerates into a temporary folder and fails if the result differs from the
committed code, so a spec update cannot be forgotten.

## Beta compatibility and the six per-asset operations

Of the 16 beta operations, about 10 have a GA counterpart. The mapping below is inferred from
the specs, since Cisco's mapping guide is only linked from the announcement email.

| Beta | GA | Confidence |
| --- | --- | --- |
| Assets list and get | `physicalAssets` list and get | high |
| Contracts list and get | `coverages` list and get | high |
| Security advisories list and get | `securityAdvisories` list and get | high |
| Field notices list and get | `fieldNotices` list and get | high |
| Asset lifecycle | End-of-life bulletins and asset fields | low, inferred |
| Asset relationships | `physicalAssets/{id}/children` and `parent` | low, inferred |
| Advisories for an asset; affected assets; affected asset (advisories) | none documented | none |
| Field notices for an asset; affected assets; affected asset (field notices) | none documented | none |

For the six unmatched operations, two rebuilds are plausible: Assessment `findings` filtered by
asset and by the `cisco.assessment.securityAdvisories` or `cisco.assessment.fieldNotices` tags,
or matching each advisory's or field notice's `affectedProducts` against an asset's `productId`.
Neither is documented, and findings exist only once an assessment has run. These are **not**
shipped as if they were official operations. If added, they go in a clearly named helper layer
and say how they are derived.

## Testing

- Unit tests per generated interface using a fake `HttpMessageHandler`: request path, query
  string, headers, body and parsing, driven by fixtures. Fixtures are written from the spec
  examples and then replaced with sanitised live captures where available.
- Hand-written layer: auth handler, host and region selection, paging, query-length guard,
  problem parsing, retry and `Retry-After`, redaction of the key.
- The generator has its own tests, plus the drift test.
- CI keeps its 100% line and branch gate. Generated code is excluded from the coverage gate and
  covered instead by the generator and drift tests; this is a change to the coverage
  configuration and needs reviewer approval (open question 4).
- Live tests carry `Category=Integration`, are excluded from the unit run, and are **read-only**.
  Write, delete and key operations are never run against a live account in the automated
  suite. They require an API key supplied through user secrets.
- Tests are written before the code they cover.

## Risk

| Risk | Mitigation |
| --- | --- |
| 214 operations, none verified live | Read-only live smoke tests for each list and get; spec-driven unit tests for the rest |
| Hosts disagree between documentation and specs | Global host plus `X-IQ-Region` by default; `BaseAddress` override; one live check |
| Destructive operations exposed (delete resource groups, revoke keys, create keys) | Exposed as the API defines them; documented as destructive; never run live in CI |
| Spec version is `0.1.0` although the changelog says `1.0.0` | The generator records the spec version; the drift test catches a change |
| Beta users lose service on 30 November | Ship the parity subset of the GA client first (milestone 1) |
| Cisco publishes a corrected spec | Re-run the generator; the diff shows exactly what changed |

## Milestones

Beta stops on 30 November 2026, so the order is chosen to get parity out first.

1. **Core and parity.** Generator, hand-written client, auth, paging, errors, and the Asset,
   Product and Assessment read operations. This replaces everything beta did.
2. **Writes.** Asset `PATCH` and `PUT`, resource groups, user preferences, contact requests,
   feedback, bulk and query.
3. **IAM.** The 106 IAM operations.
4. **Documentation.** README, migration guide from beta, samples, and the reference notes in
   `documentation/cisco-iq/` rewritten for GA.
5. **Beta removal.** Delete the obsolete beta client after 30 November 2026 and release.

Milestones 1 and 4 are the release candidates for the 30 November deadline. Milestones 2 and 3
can follow in later releases of the same package.

## Open questions

1. **Mapping guide.** Cisco's beta-to-GA mapping guide has not been read. It decides lifecycle,
   relationships and the six per-asset operations.
2. **Hosts.** Which regional hosts exist, and does the global host with `X-IQ-Region` work for
   all three regions? Needs one authenticated call.
3. **Operation count.** The files hold more `operationId` entries than Cisco's 214 because some
   repeat. The generator will reconcile by method and path.
4. **Coverage gate.** Excluding generated code from the 100% gate needs approval, because the
   repository rules say not to weaken coverage thresholds.
5. **Version number.** Whether adding a new client namespace and obsoleting the old one is a
   minor or a major release.
6. **`max` ceiling.** The documentation allows 50000 on lists; the System query endpoint allows
   100. Whether the server honours 50000 is untested.
7. **IAM and System coverage.** The first analysis covered Assessment, Asset and Product in
   detail. IAM and System request bodies and schemas still need reading.

## Implementation status (2026-11-10)

| Milestone | State |
| --- | --- |
| 1. Core and parity | Done and merged: generator, client, authentication, paging, errors, Asset, Product and Assessment reads |
| 2. Writes | Done: all PATCH, PUT, POST and DELETE operations, with JSON Merge Patch members as `CiscoIqV1Optional<T>` and the System query endpoint paged through its bodies |
| 3. IAM | Done: all IAM operations generated and exercised against the specification |
| 4. Documentation | Done: README, migration table, reference notes marked as beta |
| 5. Beta removal | Not started: the beta client is obsolete and is removed after 30 November 2026 |

Resolved open questions: generated code is excluded from both coverage reports (approved), and the `Refit.Reflection` dependency is not needed once CSV query lists are plain `List<string>`.

Still open: the mapping guide has not been read, the regional hosts are unconfirmed, and nothing has been verified against a live account.
