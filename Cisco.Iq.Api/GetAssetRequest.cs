using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAssetAsync.</summary>
public sealed record GetAssetRequest : IRequest<Asset>
{
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
}
