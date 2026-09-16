# Cisco IQ — authentication

Cisco IQ uses a **two-stage** scheme. A long-lived token is exchanged for a short-lived access
token, and only the short-lived token is ever sent to a product API.

This differs from every other API already in `Cisco.Api`, which use OAuth2 client-credentials
against `id.cisco.com` or `api.cisco.com/pss/token`. None of those credentials work here.

## The two long-lived token types

| Type | Stands for | Created where | Use for |
| --- | --- | --- | --- |
| **PAT** (Personal Access Token) | a specific human user | *Your name > User Settings > Generate Token* | user-owned scripts, exploratory testing |
| **SAT** (Service Account Token) | a service account | *Home > System Settings > Identity and Access > Add User*, type *Service Account* | machine-to-machine, CI/CD, scheduled jobs |

Both are shown **once** at creation and never displayed again.

SAT availability is restricted: customer-type accounts with the Account Administrator role only.
Partner-type accounts and partner roles cannot create service accounts, and must use PATs.

A service account supports **up to five** concurrent long-lived tokens (including the one issued
at account creation), managed from *Manage API Tokens* on the service account. This is what makes
zero-downtime rotation possible — create the replacement, cut over, then revoke the old one.

## Stage 1 — exchange the long-lived token

```http
POST https://iq.cisco.com/cxp-iam/api/v1/auth/issueToken
Authorization: Basic <PAT-or-SAT>
Content-Type: application/json
Accept: application/json
Cookie: account_region=<US|EMEA|APJC>

{ "accountId": "<cisco-iq-account-id>" }
```

Response:

```json
{ "accessToken": "<short-lived-access-token>", "expiresInSeconds": 3600 }
```

### Three things that will bite an implementer

1. **`Basic <PAT>` is not HTTP Basic auth.** Cisco's own agent guidance is explicit: the raw
   PAT or SAT value goes directly after `Basic`. Do **not** Base64-encode a `user:password`
   pair. `HttpClient`'s `AuthenticationHeaderValue("Basic", token)` does the right thing here
   precisely because it does no encoding — but any helper that "helpfully" encodes will break it.
2. **The exchange endpoint is on a different path prefix** — `/cxp-iam/api/v1`, not
   `/ciq-rest/api/v0`. Same host.
3. **The `account_region` cookie is required on the exchange too**, not just on product calls.

### `accountId`

- **PAT: required.** A human user may have access to several Cisco IQ accounts; the `accountId`
  selects which one. Each resulting access token is scoped to exactly one account, so working
  across accounts means one exchange (and one cached token) per account.
- **SAT: optional.** A service account belongs to one account already. If supplied it must match
  that account. An empty body `{}` is valid.

Find the Account ID and Data Storage Region at *Home > System Settings > Account Details*.

## Stage 2 — call a product API

### Live verification: User-Agent

On 2026-09-16, a bare .NET `HttpClient` token exchange without a `User-Agent`
returned HTTP 403 with an HTML response served by CloudFront. The same credentials,
account ID, EMEA cookie and JSON body succeeded with HTTP 200 after adding
`User-Agent: Cisco.Iq.Api/1.0`. A subsequent `GET /assets?max=1` using that
User-Agent also succeeded with HTTP 200.

Send a nonempty default User-Agent on both the exchange and product clients, allowing
callers to override it through `CiscoIqClientOptions.UserAgent`. An HTML 403 from
CloudFront is distinct from the documented JSON authorization error; check the
User-Agent before assuming the PAT or account permissions are wrong. The observations
above do not establish the exact CloudFront rule.

Live verification also confirmed the exchange's `expiresInSeconds` was 3600 and that
Asset, AssetLifecycle and AssetRelationship payload fields matched the reference notes.
The account's contract, security advisory and field notice collections were empty, so
those item schemas and AffectedAsset have not yet been verified against live records.

```http
GET https://iq.cisco.com/ciq-rest/api/v0/assets?max=10
Authorization: Bearer <short-lived-access-token>
Accept: application/json
Cookie: account_region=<US|EMEA|APJC>
```

Sending a PAT or SAT directly to a product endpoint is explicitly disallowed.

## `account_region`

Required on **every** request, exchange and product alike, as a **cookie** — not a header or
query parameter. Allowed values are `US`, `EMEA` and `APJC`; it is the account's Data Storage
Region and it routes the request to the right data residency zone.

It is the awkward part of this API for a typed client: Refit has no cookie parameter, so it has
to be set as a `Cookie` request header. If an `HttpClientHandler` is used with its default
`UseCookies = true`, the handler owns the cookie container and will discard a manually set
`Cookie` header — so `UseCookies` must be set to `false`.

## Expiry, rotation, revocation

- `expiresInSeconds` was `3600` in every documented example, but it is a response field and
  should be read rather than assumed.
- Expired, revoked, deleted or malformed tokens all return **401**. There is no distinct status
  for "expired" — on a 401, re-exchange and retry once.
- Rotating a PAT/SAT: create the replacement, update the integration, confirm it can exchange
  and call successfully, then revoke the old one. Revoking first breaks the integration.

Cisco's stated storage practice: keep tokens in a secrets manager; never in URLs, screenshots,
logs, source code, or shared documents.

## Authorization is separate from authentication

A token carries the effective permissions of the user or service account behind it. There are
**no API-specific scopes**. What you can see is determined by:

- the identity's **role** — `Administrator` (full access in the account) or `Viewer` (read-only)
- **resource groups**, which narrow a `Viewer` to a selected subset of assets
- **user group** memberships, which apply role and resource-group assignments in bulk
- the **account context** fixed by the access token
- Cisco's **entitlement** rules for which asset and assessment data is visible at all

A valid token that lacks access returns **403**, not 401.

**Access changes propagate slowly** — "a few minutes to almost an hour". A permissions change
that appears not to have worked may simply not have landed yet; that is worth knowing before
debugging a 403 that is about to resolve itself.
