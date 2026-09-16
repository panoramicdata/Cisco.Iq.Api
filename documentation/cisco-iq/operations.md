# Cisco IQ — operations

All 16 operations, all `GET`, all under `https://iq.cisco.com/ciq-rest/api/v0`.
Every one requires `Authorization: Bearer <short-lived-access-token>` and the
`account_region` cookie.

Parameters shared by every collection endpoint — `max` (default 50, 1–200), `offset`
(default 0), `sort`, `order` (`ASC`/`DESC`), `fields` — are listed once here and not repeated
per operation below. Defaults for `sort` do vary and are called out.

---

## Assets (`Assets.json`, 6 operations)

### `GET /assets` — `listAssets`

Returns `{ items: Assets[], meta }`. Default sort `serialNumber`.

By some distance the largest surface in the API: **51 query parameters**. They group as:

**Identity and classification** (all arrays, repeat to supply multiple values)
`productFamily`, `productId`, `serialNumber`, `hostname`, `ipAddress`, `equipmentType`,
`productType`, `softwareType`, `softwareVersion`, `role`, `importance`, `location`,
`assetTags`, `dataSource`

**Contract and support** (arrays)
`contractNumber`, `contractId`, `contractStatus`, `coverageStatus`, `supportType`,
`supportTier`, `partnerName`

**Telemetry** (arrays)
`telemetryStatus`, `lastSignalType`

**Lifecycle milestones** (arrays)
`currentHardwareMilestone`, `currentSoftwareMilestone`, `nextHardwareMilestone`,
`nextSoftwareMilestone`

**Date ranges** (nullable `int64`, epoch **milliseconds**, in `Before`/`After` pairs)
`lastSignalBefore` / `lastSignalAfter`, `coverageEndBefore` / `coverageEndAfter`,
`warrantyEndBefore` / `warrantyEndAfter`, `shipDateBefore` / `shipDateAfter`,
`endOfSoftwareMaintenanceBefore` / `endOfSoftwareMaintenanceAfter`,
`nextHardwareMilestoneDateBefore` / `nextHardwareMilestoneDateAfter`,
`nextSoftwareMilestoneDateBefore` / `nextSoftwareMilestoneDateAfter`,
`hardwareLastDateOfSupportBefore` / `hardwareLastDateOfSupportAfter`,
`softwareLastDateOfSupportBefore` / `softwareLastDateOfSupportAfter`

**Boolean**
`hasCriticalOrHighSecurityAdvisories` (nullable)

> A 51-parameter Refit method signature is unmanageable. This is the main argument for a
> filter/request object with a custom URL serializer rather than one parameter per filter.

Responses: `200`, `400`, `401`, `403`, `406`, `429`, `502`.

### `GET /assets/{assetId}` — `getAssetById`

`assetId` is a **string**. Returns a single `Assets`. Adds `404`.

### `GET /assets/{assetId}/lifecycle` — `getAssetLifecycle`

Query: `milestoneType` — `hardware` (default) or `software`. The only real enum in the API.
Returns a single `AssetLifecycle`, not a collection — no paging parameters. Adds `404`.

### `GET /assets/{assetId}/relationships` — `getAssetRelationship`

Returns `{ items: AssetRelationships[], meta }` — an asset's parent and child relationships.
Default sort `assetId`. Collection parameters only; no filters.

### `GET /contracts` — `listContracts`

Returns `{ items: Contracts[], meta }`. Default sort `contractStartDate`.

Filters: `contractNumber`, `contractStatus`, `serviceLevel`, `supportTier`, `partnerName`
(arrays); `contractEndBefore` / `contractEndAfter` (epoch ms).

### `GET /contracts/{contractNumber}` — `getContract`

`contractNumber` is a **string**. Returns a single `Contracts`. Adds `404`.

---

## Assessments (`Assessments.json`, 10 operations)

Note the identifier types: `psirtId` is an **integer**, `fieldNoticeId` is an **integer**,
`assetId` is a **string**. Easy to get wrong.

### Security advisories

#### `GET /securityAdvisories` — `listSecurityAdvisories`

Returns `{ items: SecurityAdvisories[], meta }`. Default sort `psirtId`.
Filter: `impact` (array).

#### `GET /securityAdvisories/{psirtId}` — `getSecurityAdvisoryById`

Single `SecurityAdvisories`. Adds `404`.

#### `GET /securityAdvisories/{psirtId}/assets` — `listSecurityAdvisoryAffectedAssets`

Assets affected by an advisory. Returns `{ items: AffectedAssets[], meta }`.
Default sort `assetId`. Collection parameters only.

#### `GET /securityAdvisories/{psirtId}/assets/{assetId}` — `getSecurityAdvisoryAffectedAssetById`

One affected asset. Single `AffectedAssets`. Adds `404`.

#### `GET /assets/{assetId}/securityAdvisories` — `listSecurityAdvisoriesPerAsset`

The reverse view — advisories impacting one asset. Returns
`{ items: SecurityAdvisories[], meta }`. Default sort `psirtId`.
Filters: `impact`, `vulnerabilityStatus` (both arrays).

### Field notices

The field notice operations mirror the advisory ones exactly.

#### `GET /fieldNotices` — `listFieldNotices`

Returns `{ items: FieldNotices[], meta }`. Default sort `fieldNoticeId`.
Filter: `impact` (array).

#### `GET /fieldNotices/{fieldNoticeId}` — `getFieldNoticeById`

Single `FieldNotices`. Adds `404`.

#### `GET /fieldNotices/{fieldNoticeId}/assets` — `listFieldNoticeAffectedAssets`

Returns `{ items: AffectedAssets[], meta }`. Default sort `assetId`.

#### `GET /fieldNotices/{fieldNoticeId}/assets/{assetId}` — `getFieldNoticeAffectedAssetById`

Single `AffectedAssets`. Adds `404`.

#### `GET /assets/{assetId}/fieldNotices` — `listFieldNoticesPerAsset`

Returns `{ items: FieldNotices[], meta }`. Default sort `fieldNoticeId`.
Filters: `impact`, `vulnerabilityStatus` (both arrays).

---

## Symmetry worth exploiting

The ten assessment operations are two identical five-operation sets — advisories keyed by
`psirtId`, field notices keyed by `fieldNoticeId` — over the same `AffectedAssets` type. Both
"list findings for an asset" operations take the same `impact` + `vulnerabilityStatus` filters.

That symmetry should show up in the client's shape rather than being written out twice.
