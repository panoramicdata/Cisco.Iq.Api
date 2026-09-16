using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetSecurityAdvisoriesForAssetAsync.</summary>
public sealed record GetSecurityAdvisoriesForAssetRequest : IRequest<CiscoIqPage<SecurityAdvisory>>
{
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
	/// <summary>The filter for this request.</summary>
	public SecurityAdvisoryFilter? Filter { get; init; }
}
