using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetSecurityAdvisoriesAsync.</summary>
public sealed record GetSecurityAdvisoriesRequest : IRequest<CiscoIqPage<SecurityAdvisory>>
{
	/// <summary>The filter for this request.</summary>
	public SecurityAdvisoryFilter? Filter { get; init; }
}
