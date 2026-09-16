# Cisco IQ — collection conventions, rate limits and errors

Conventions shared by every collection endpoint. Which filters and sort fields a given endpoint
accepts varies — see [operations.md](operations.md).

## Collection response shape

```json
{
  "items": [ ... ],
  "meta": { "count": 18974, "max": 50, "offset": 50 }
}
```

`items` and `meta` are both always present and both required.

## Pagination

| Parameter | Default | Allowed |
| --- | --- | --- |
| `max` | 50 | 1–200 |
| `offset` | 0 | ≥ 0 |

### Counting

The total matching the active filters appears in two places: `meta.count` in the body and the
`X-Total-Count` response header. Both reflect the **filters**, not the page size — `max` and
`offset` do not affect them.

**`count` can be `null`.** When Cisco IQ cannot determine a total, `meta.count` is `null` and
the `X-Total-Count` header is omitted entirely. Cisco's guidance is explicit: treat a null count
as *total unknown*, never as zero, and never use it to decide whether more pages exist.

This matters for a typed client — `meta.count` must be modelled as `long?`/`int?`, and any
"fetch all pages" helper must not terminate on `count == 0`.

### Finding the next page

When another page exists the response carries an RFC 8288 `Link` header with a **relative**
URL, resolved against the request URI:

```http
Link: </ciq-rest/api/v0/assets?offset=50&max=50>; rel="next"
```

Relations are `next` and `prev` only — Cisco does not provide `first` or `last`. The header is
absent when there is no such page, and **its absence is the reliable end-of-collection signal**.

Cisco asks callers to follow the supplied URL rather than constructing the next one themselves.

## Sorting

`sort` names a property; `order` is `ASC` (default) or `DESC`. The default `sort` differs per
endpoint (`serialNumber` for assets, `psirtId` for advisories, `fieldNoticeId` for field
notices, `assetId` for affected-asset and relationship collections, `contractStartDate` for
contracts).

Note that the downloadable OpenAPI definitions do **not** enumerate the allowed `sort` values,
but the documentation does for `/assets`: `serialNumber`, `productFamily`, `productId`,
`hostname`, `lastSignalDate`, `coverageEndDate`, `contractNumber`, `location`, `equipmentType`,
`productType`, `supportTier`, `partnerName`. Treat `sort` as a free string in the client rather
than an enum — the spec does not constrain it and the documented list is not guaranteed complete
for other endpoints.

## Filtering

Array-valued filters are `style: form`, `explode: true` — **repeat the parameter**:

```http
GET /ciq-rest/api/v0/assets?productId=<value-1>&productId=<value-2>
```

Date filters are **Unix epoch milliseconds** as `int64`, and come in `...Before` / `...After`
pairs (`coverageEndBefore`, `coverageEndAfter`, and so on).

## Field selection

`fields` takes a comma-separated list of properties to return; omit it to get all of them. Only
properties defined in that endpoint's response schema are valid.

A sparse response is the same schema with most properties absent — so every field on every
response model needs to be nullable/optional regardless of what the schema marks as required.

## Rate limits

| Scope | Per second | Per 24 hours |
| --- | --- | --- |
| User (human user *or* service account) | 10 | 5,000 |
| Cisco IQ account | 25 | 25,000 |

All active credentials for the same user share the user limit; all users in an account share the
account limit. **A request must satisfy both.**

Cisco frames these as fair-use safeguards, not billing limits or service-level commitments.

### Rate-limit headers

Successful responses carry all twelve, being the cross product of
{`principal`, `account`} × {`second`, `day`} × {`limit`, `remaining`, `reset`}:

```
x-principal-second-ratelimit-limit / -remaining / -reset
x-principal-day-ratelimit-limit    / -remaining / -reset
x-account-second-ratelimit-limit   / -remaining / -reset
x-account-day-ratelimit-limit      / -remaining / -reset
```

`reset` is **seconds until that window resets**. When a `remaining` reaches 0, wait out the
corresponding `reset` before retrying.

A `429` includes headers for the window that was exceeded, and may or may not include the others
— so a client cannot assume all twelve are present on a 429.

### 429 body

```json
{ "message": "Rate limit exceeded. Retry after the exhausted rate-limit window resets." }
```

Message text may vary; don't match on it.

## Errors

| Status | Meaning | Retry? |
| --- | --- | --- |
| `400` | Request failed validation | No — fix the request |
| `401` | Auth missing, invalid or expired | No — re-exchange the PAT/SAT, then retry once |
| `403` | Authenticated but not authorized | No — check role, resource groups, entitlements |
| `404` | No resource matched the identifier | No. Only on operations that document it |
| `406` | Unsupported response content type | No — send `Accept: application/json` |
| `429` | Rate limit exceeded | Yes — after the exhausted window's `reset` |
| `502` | Upstream service failed | Yes — bounded exponential backoff with jitter |

Only `429` and `502` are retryable. Cisco is explicit: bound every retry by a maximum count or
elapsed time, and never retry indefinitely.

### Error body

```json
{ "message": "You are not authorized to access this resource.", "trackingId": "..." }
```

`message` is required; `trackingId` is optional. A `TrackingID` **response header** may also be
present, independently of the body field — preserve both when they appear, as they are what
Cisco TAC uses to trace a request.

### Note on 404

Only single-resource operations declare `404`. Collection operations declare
`400/401/403/406/429/502` but **not** `404` — an empty collection comes back as `200` with an
empty `items` array.
