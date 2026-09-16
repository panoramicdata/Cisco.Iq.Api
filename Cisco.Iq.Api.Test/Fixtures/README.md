# Fixture provenance

The model-named JSON files in this directory are synthetic contract examples based on
the repository's API reference notes. They exercise every declared field, including
millisecond dates, arrays, decimal scores and the differing advisory/notice count types.
They are our own test data, not Cisco's OpenAPI definitions or documentation artifacts.

`Captured/` contains sanitized live responses from 2026-09-16: the token exchange and
Asset, AssetLifecycle and AssetRelationship records. All string values, including tokens,
serial numbers, hostnames, IP addresses, asset/customer identifiers and URLs, were replaced
before writing these files. Timestamp and numeric field representations were preserved.

The account's contract, security advisory and field notice collections returned HTTP 200
with no items. Consequently those item schemas and AffectedAsset use synthetic examples;
no live payload is claimed for those four models. Replace or supplement them with sanitized
live records when an account with suitable data becomes available.

No PAT, SAT, real access token or original Cisco documentation download is retained here.
