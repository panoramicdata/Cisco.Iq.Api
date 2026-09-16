# Handover prompt

Paste the block below into a fresh session that has web access and a checkout of this
repository. It is written for the agent doing the implementation, not for a human reader.

---

You are implementing a complete .NET client for the Cisco IQ REST API in
`https://github.com/panoramicdata/Cisco.Iq.Api`. Clone it if you do not already have it.

**Read these three things before writing any code.** They are the specification, and the design
decisions in them are settled — implement them, do not redesign them.

1. GitHub issue #1 in that repo — the scope, the definition of done, and the three failure modes
   that will otherwise cost you hours
2. `docs/superpowers/specs/2026-09-16-cisco-iq-client-design.md` — the full design
3. `documentation/cisco-iq/` — reference notes on the API: authentication, collection
   conventions, all 16 operations, all seven response schemas

The repository today is scaffolding that builds and tests green. There is no real code yet
beyond `CiscoIqAccountRegion`.

## Task 1 — get credentials working, before anything else

Nothing can be verified against the live API until this is done, so do it first.

The Cisco IQ API needs a long-lived **Personal Access Token (PAT)** or **Service Account Token
(SAT)**, which only exists after someone generates it in the Cisco IQ web UI. You cannot log in
as the user. Walk them through it and wait:

1. Ask them to log in to <https://iq.cisco.com/>
2. **For a PAT:** click their name, top right → **User Settings** → **Generate Token** → give it
   a name → **Generate Token**
   **For a SAT** (better for automation, but only available to customer-type accounts with the
   Account Administrator role — partner accounts must use a PAT): **Home → System Settings →
   Identity and Access → Add User** → user type **Service Account** → name it → assign
   **Administrator**, or **Viewer** plus resource groups → **Save**
3. Tell them to copy the token immediately — **Cisco never displays it again**
4. Ask them to go to **Home → System Settings → Account Details** and read off the **Account ID**
   and the **Data Storage Region** (`US`, `EMEA` or `APJC`)

Then store all three in user secrets. `UserSecretsId` is already set on the test project:

```bash
cd Cisco.Iq.Api.Test
dotnet user-secrets set "CiscoIq:Token" "<the-PAT-or-SAT>"
dotnet user-secrets set "CiscoIq:AccountId" "<the-account-id>"
dotnet user-secrets set "CiscoIq:AccountRegion" "<US|EMEA|APJC>"
```

Ask the user to run those commands themselves, with the real values, rather than having them
paste the token to you. If they would rather you ran them, that is their call — but never echo
the token back, never write it to a file in the repo, and never put it in a commit, a log line,
a test fixture or a GitHub comment.

Then prove it works end to end before building on it — exchange the token and make one real
call:

```bash
curl -sS -X POST 'https://iq.cisco.com/cxp-iam/api/v1/auth/issueToken' \
  -H 'Authorization: Basic <PAT-or-SAT>' \
  -H 'Content-Type: application/json' -H 'Accept: application/json' \
  --cookie 'account_region=<REGION>' \
  -d '{"accountId":"<ACCOUNT-ID>"}'

curl -sS 'https://iq.cisco.com/ciq-rest/api/v0/assets?max=1' \
  -H 'Authorization: Bearer <accessToken-from-above>' \
  -H 'Accept: application/json' --cookie 'account_region=<REGION>'
```

If the second call 403s, the identity may lack entitlements — note that Cisco says role and
resource-group changes take "a few minutes to almost an hour" to propagate, so a 403 immediately
after a permissions change may resolve itself.

Capture the real response payloads (**sanitize serial numbers, hostnames, IPs and customer ids**)
and use them as test fixtures. They are more trustworthy than the spec examples.

## Task 2 — implement the client

Work through the definition of done in issue #1. Test-driven: write the failing test first.

The three things that will bite you, each of which needs its own regression test:

1. **Set `HttpClientHandler.UseCookies = false`.** `account_region` is a mandatory cookie and
   Refit has no cookie parameter, so it goes on as a `Cookie` header — which .NET's cookie
   container silently discards at the default `UseCookies = true`. The symptom is a `400` on
   every single call with nothing in the error explaining why.
2. **`Authorization: Basic <PAT>` is the raw token, not HTTP Basic auth.** Do not Base64-encode
   a `user:password` pair. `new AuthenticationHeaderValue("Basic", token)` is right precisely
   because it encodes nothing.
3. **`meta.count` is nullable and null means *unknown*, not zero.** Never use it to decide
   whether more pages exist. Use the absence of an RFC 8288 `Link` header with `rel="next"`, and
   follow the URL that header gives you rather than incrementing `offset` yourself.

Mind the rate limits while testing against the live API: 10 requests/second and 5,000/day per
user, 25/second and 25,000/day per account. Both apply. Do not hammer it in a loop.

## Task 3 — finish properly

- `dotnet build` and `dotnet test` both clean. `TreatWarningsAsErrors` is on, so warnings fail
  the build.
- Delete `Cisco.Iq.Api.Test/ScaffoldingTests.cs`.
- Integration tests must **skip**, not fail, when `CiscoIq:Token` is absent — a fresh clone with
  no credentials has to be green.
- Update `README.md` so its usage examples match what you actually shipped. The beta warning
  stays in `README.md` and **only** there — no `[Experimental]` attribute, no XML doc remarks
  about beta, no `PackageReleaseNotes` line.
- Work on a branch, open a PR against `main`, and confirm CI is green before asking for review.

## Using your web access

Cisco's own documentation is at <https://iq.cisco.com/api/v1/apiregistry/docs/intro> and is
**login-gated** — an unauthenticated fetch 302s to a login page. If you need something from it
beyond what `documentation/cisco-iq/` already records, ask the user to paste the section or to
download the `Assets.json` / `Assessments.json` OpenAPI definitions and the `llms-full.txt`
context artifact from the *API Specification Downloads* section.

Those three files are deliberately **not** committed to this repository — they are Cisco's
copyright and this is a public MIT package. Read them locally if the user supplies them; do not
commit them.

## Ground rules

- Never commit, log, echo or paste a token anywhere. `appsettings.json` is gitignored as a
  backstop, not as the intended store — user secrets is.
- The API is read-only. There is no sandbox, so integration tests run against production; keep
  `max` small and the request count low.
- Cisco classifies this API as beta on a `v0` path with no backward-compatibility guarantee. If
  the live API disagrees with the spec, **trust the live API**, and record the discrepancy in
  `documentation/cisco-iq/` so the next person knows.
