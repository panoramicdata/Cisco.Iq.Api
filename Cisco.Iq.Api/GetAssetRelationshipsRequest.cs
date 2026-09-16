using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAssetRelationshipsAsync.</summary>
public sealed record GetAssetRelationshipsRequest : IRequest<CiscoIqPage<AssetRelationship>>
{
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
	/// <summary>The filter for this request.</summary>
	public CiscoIqFilter? Filter { get; init; }
}
