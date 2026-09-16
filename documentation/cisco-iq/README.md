# Cisco IQ API — reference notes

Working notes on the Cisco IQ REST API, written up for the `Cisco.Iq.Api` implementation.
These are our own summaries of Cisco's developer documentation, not a copy of it.

**Source of truth:** <https://iq.cisco.com/api/v1/apiregistry/docs/intro> (requires a Cisco IQ login).
Cisco also publishes an `llms-full.txt` context artifact and downloadable `Assets.json` /
`Assessments.json` OpenAPI 3.1 definitions from the API Registry's *API Specification Downloads*.
Everything below was derived from those, as of the `2026-07-24` API release
(artifact generated `2026-08-31`, API version `0.1.0`).

## Contents

| Document | Covers |
| --- | --- |
| [authentication.md](authentication.md) | PAT/SAT token exchange, the `account_region` cookie, expiry and rotation |
| [conventions.md](conventions.md) | Pagination, sorting, filtering, field selection, rate limits, errors |
| [operations.md](operations.md) | All 16 operations with their parameters |
| [schemas.md](schemas.md) | Response schemas and field types |

## What Cisco IQ is

An AI-assisted interface over Cisco Support and Professional Services data — asset inventory,
contracts and coverage, hardware/software lifecycle milestones, security advisory (PSIRT)
exposure, and field notice impact. The API exists so this data can be pulled into a CMDB,
ITSM system, reporting stack or automation without exporting from the UI.

## Scope of the current release

- **16 operations, all `GET`.** Read-only.
- Two functional areas: **Assets** (6 operations) and **Assessments** (10 operations).
- Base URL: `https://iq.cisco.com/ciq-rest/api/v0`
- Cisco IQ SaaS production only. No sandbox.

Explicitly **not** provided: write operations, webhooks or event notifications, official SDKs
or client libraries, a testing sandbox, and any uptime/performance/SLA commitment.

## Beta status — read this before shipping

Cisco labels these APIs **beta (public preview)** and states plainly:

> API behavior (including endpoint paths, request and response schemas, authentication,
> pagination, error handling, and other semantics) may change between releases without
> maintaining backward compatibility. Do not build production integrations against the beta APIs.

"Beta" refers to API maturity, not to limited availability — the APIs are generally available
to anyone with a Cisco IQ account. But the absence of a backward-compatibility guarantee is
real, and the `v0` base path reflects it. Anything this package exposes should be
documented as beta and expected to churn.

## Licensing

The Cisco IQ API itself is governed by the
[Cisco API License](https://developer.cisco.com/site/license/cisco-api-license/). That covers
use of the *service*, and does not constrain the licensing of a third-party client library —
`Cisco.Iq.Api` remains MIT. Cisco's documentation text and OpenAPI definitions are Cisco's
copyright, which is why this folder holds our own summaries rather than verbatim copies.
