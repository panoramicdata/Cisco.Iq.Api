using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetFieldNoticesForAssetAsync.</summary>
public sealed record GetFieldNoticesForAssetRequest : IRequest<CiscoIqPage<FieldNotice>>
{
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
	/// <summary>The filter for this request.</summary>
	public FieldNoticeFilter? Filter { get; init; }
}
