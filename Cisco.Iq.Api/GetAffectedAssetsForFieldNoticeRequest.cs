using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAffectedAssetsForFieldNoticeAsync.</summary>
public sealed record GetAffectedAssetsForFieldNoticeRequest : IRequest<CiscoIqPage<AffectedAsset>>
{
	/// <summary>The fieldNoticeId for this request.</summary>
	public required int FieldNoticeId { get; init; }
	/// <summary>The filter for this request.</summary>
	public CiscoIqFilter? Filter { get; init; }
}
