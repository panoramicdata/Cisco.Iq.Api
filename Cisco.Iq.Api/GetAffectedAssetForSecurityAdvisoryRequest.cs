using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAffectedAssetForSecurityAdvisoryAsync.</summary>
public sealed record GetAffectedAssetForSecurityAdvisoryRequest : IRequest<AffectedAsset>
{
	/// <summary>The psirtId for this request.</summary>
	public required int PsirtId { get; init; }
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
}
