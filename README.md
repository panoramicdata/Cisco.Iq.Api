# Cisco.Iq.Api

[![Nuget](https://img.shields.io/nuget/v/Cisco.Iq.Api)](https://www.nuget.org/packages/Cisco.Iq.Api/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A .NET library for the **Cisco IQ** Assets and Assessments REST APIs — asset inventory,
contracts and coverage, hardware and software lifecycle milestones, security advisory (PSIRT)
exposure, and field notice impact.

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

using var client = new CiscoIqClient(new CiscoIqClientOptions
{
    Token = "<your-PAT-or-SAT>",
    AccountRegion = CiscoIqAccountRegion.Emea,
    AccountId = "<your-account-id>"  // required for a PAT, optional for a SAT
});

// Assets covered by a contract, most recently seen first
var page = await client.Assets.GetAssetsAsync(new AssetFilter
{
    CoverageStatus = ["COVERED"],
    Sort = "lastSignalDate",
    Order = CiscoIqSortOrder.Descending,
    Max = 200
}, cancellationToken);

foreach (var asset in page.Items)
{
    Console.WriteLine($"{asset.SerialNumber} {asset.ProductId} {asset.HardwareLastDateOfSupport:d}");
}

// Every asset affected by a critical advisory, paging handled for you
await foreach (var affected in client.Assessments
    .GetAffectedAssetsForSecurityAdvisoryAsync(psirtId: 82456, cancellationToken))
{
    Console.WriteLine(affected.Hostname);
}
```

The library handles the two-stage token exchange, the mandatory `account_region` cookie, access
token caching and renewal, `Link`-header pagination, and rate-limit backoff.

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
