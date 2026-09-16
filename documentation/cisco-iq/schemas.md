# Cisco IQ — response schemas

Seven schemas across the two definitions. `ErrorBody` and `CollectionMeta` are declared
identically in both.

## Conventions that apply to all of them

- **Dates are Unix epoch milliseconds**, typed `integer` / `format: int64`. Not seconds, not
  ISO 8601. A `DateTimeOffset` conversion must use `FromUnixTimeMilliseconds`.
- **Almost every property is nullable.** The OpenAPI 3.1 definitions use `"type": ["string", "null"]`
  for most fields. Combined with the `fields` sparse-selection parameter, any property can be
  absent from any response — so every field on every model should be optional in the client,
  including the handful the spec marks required.
- Free-text status-like fields (`coverageStatus`, `impact`, `vulnerabilityStatus`,
  `telemetryStatus`, `currentHardwareMilestone`, …) are **plain strings with no declared enum**.
  Observed values include `COVERED`, `Critical`, `VUL`, `Last Date of Support`, `End of Sale` —
  note the inconsistent casing between them. Modelling these as C# enums would be guessing, and
  a beta API is exactly where an unexpected value will appear. Keep them as strings.

---

## `ErrorBody`

| Property | Type | Required |
| --- | --- | --- |
| `message` | string | yes |
| `trackingId` | string | no |

## `CollectionMeta`

| Property | Type | Notes |
| --- | --- | --- |
| `count` | integer \| null | total matching the filters; **null means unknown, not zero** |
| `max` | integer | page size in effect |
| `offset` | integer | zero-based offset in effect |

---

## `Assets`

The main inventory record — 42 properties.

**Identity:** `assetId`, `customerId`, `serialNumber`, `productId`, `productFamily`,
`productDescription`, `productType`, `equipmentType`

**Network:** `hostname`, `ipAddress`

**Software:** `softwareVersion`, `softwareType`

**Operational classification:** `role`, `importance`, `location`, `tags` (string array)

**Support and contract:** `coverageStatus`, `contractNumber`, `coverageEndDate`,
`supportType`, `supportTier`, `partnerName`, `salesOrderNumber`, `warrantyType`,
`warrantyEndDate`

**Hardware lifecycle:** `currentHardwareMilestone`, `currentHardwareMilestoneDate`,
`nextHardwareMilestone`, `nextHardwareMilestoneDate`, `hardwareLastDateOfSupport`

**Software lifecycle:** `currentSoftwareMilestone`, `currentSoftwareMilestoneDate`,
`nextSoftwareMilestone`, `nextSoftwareMilestoneDate`, `softwareLastDateOfSupport`,
`endOfSoftwareMaintenance`

**Telemetry:** `lastSignalDate`, `lastSignalType`, `telemetryStatus`, `dataSource`

**Other:** `securityAdvisoryCount` (integer), `shipDate`

All date properties are epoch milliseconds.

## `AffectedAssets`

37 properties. Largely an `Assets` subset, minus the warranty, sales-order, role, importance,
product-description and `securityAdvisoryCount` fields, plus three finding-specific ones:

| Property | Type | Notes |
| --- | --- | --- |
| `bulletinId` | integer \| null | the advisory or field notice this record relates to |
| `vulnerabilityStatus` | string \| null | e.g. `VUL` |
| `vulnerabilityReasons` | string array \| null | |
| `assetType` | string \| null | |

It also carries `currentHardwareMilestone` / `currentSoftwareMilestone` but — unlike `Assets` —
**not** their `...Date` counterparts. `AffectedAssets` is therefore not assignable to `Assets`
and needs its own type.

## `AssetLifecycle`

Milestone detail for one asset, for one `milestoneType` (`hardware` or `software`).

| Property | Type |
| --- | --- |
| `assetId` (required), `serialNumber`, `productId`, `milestoneType` | string |
| `currentMilestone`, `nextMilestone` | string \| null |
| `currentMilestoneDate`, `nextMilestoneDate`, `lastDateOfSupport` | epoch ms \| null |
| `bulletinReference`, `bulletinTitle`, `bulletinUrl` | string \| null |
| `endOfLifeAnnouncementDate`, `endOfSaleDate`, `lastShipDate` | epoch ms \| null |
| `endOfRoutineFailureAnalysisDate`, `endOfNewServiceAttachmentDate` | epoch ms \| null |
| `endOfServiceContractRenewalDate`, `endOfSoftwareMaintenance` | epoch ms \| null |
| `endOfVulnerabilitySecuritySupport` | epoch ms \| null |

The generic `currentMilestone` / `nextMilestone` / `lastDateOfSupport` fields restate whichever
of the specific dates applies, for the requested `milestoneType`.

## `AssetRelationships`

| Property | Type |
| --- | --- |
| `assetId` | string (required) |
| `parentAssetId` | string \| null |
| `relationshipType` | string \| null |
| `equipmentType`, `productType`, `productId`, `serialNumber` | string \| null |

Parent/child only — chassis to module, that kind of thing.

## `Contracts`

| Property | Type |
| --- | --- |
| `customerId`, `contractNumber` | string (required) |
| `contractStatus`, `serviceLevel`, `supportTier`, `supportType` | string \| null |
| `serviceLevelAgreementDescription`, `partnerName` | string \| null |
| `contractStartDate`, `contractEndDate` | epoch ms \| null |
| `coveredAssetCount` | integer \| null |

## `SecurityAdvisories`

| Property | Type | Notes |
| --- | --- | --- |
| `psirtId` | integer | required; the API's key for an advisory |
| `advisoryId` | string | required; e.g. `cisco-sa-ios-xe-webui-privesc-j22SaA4z` |
| `title`, `description`, `additionalNotes`, `url` | string \| null | |
| `impact` | string \| null | e.g. `Critical` |
| `cvssScore`, `cvssTemporalScore` | number \| null | **not** integers |
| `cveIds` | string array \| null | |
| `ciscoBugIds` | string array \| null | |
| `alertStatusCd`, `version` | string \| null | |
| `createdAt`, `firstPublished`, `lastPublished`, `psirtLastUpdateDate` | epoch ms \| null | |
| `affectedAssetsCount` | **string** \| null | see below |
| `potentiallyAffectedAssetsCount` | **string** \| null | see below |
| `vulnerabilityStatus` | string \| null | |
| `vulnerabilityReasons` | string array \| null | |

> **Spec inconsistency worth recording.** `affectedAssetsCount` and
> `potentiallyAffectedAssetsCount` are typed `string` here but `integer` on `FieldNotices`.
> Cisco's own example returns them quoted (`"affectedAssetsCount": "8"`), so the spec looks
> correct rather than mistyped. Model them as declared and let the caller parse — silently
> coercing to `int` would break the moment a non-numeric value appears, and this is a beta API.

## `FieldNotices`

| Property | Type | Notes |
| --- | --- | --- |
| `fieldNoticeId` | integer | required |
| `title` | string | required |
| `impact` | string | required |
| `firstPublished`, `fieldNoticeLastUpdateDate` | epoch ms | required |
| `potentiallyAffectedAssetsCount` | **integer** | required — contrast with the advisory type |
| `url`, `status` | string \| null | |
| `problemDescription`, `description`, `additionalNotes`, `workaround` | string \| null | |
| `affectedAssetsCount` | **integer** \| null | |
| `lastPublished` | epoch ms \| null | |
| `ciscoBugIds` | string array \| null | |
| `vulnerabilityStatus` | string \| null | |
| `vulnerabilityReasons` | string array \| null | |

Note `fieldNoticeLastUpdateDate` here versus `psirtLastUpdateDate` on advisories — the two
types are deliberately parallel but not identically named.
