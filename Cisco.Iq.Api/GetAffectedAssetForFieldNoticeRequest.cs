using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAffectedAssetForFieldNoticeAsync.</summary>
public sealed record GetAffectedAssetForFieldNoticeRequest : IRequest<AffectedAsset>
{
	/// <summary>The fieldNoticeId for this request.</summary>
	public required int FieldNoticeId { get; init; }
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
}
