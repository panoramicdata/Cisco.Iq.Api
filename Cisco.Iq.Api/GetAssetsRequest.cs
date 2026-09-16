using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAssetsAsync.</summary>
public sealed record GetAssetsRequest : IRequest<CiscoIqPage<Asset>>
{
	/// <summary>The filter for this request.</summary>
	public AssetFilter? Filter { get; init; }
}
