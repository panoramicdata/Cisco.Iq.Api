using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetSecurityAdvisoryAsync.</summary>
public sealed record GetSecurityAdvisoryRequest : IRequest<SecurityAdvisory>
{
	/// <summary>The psirtId for this request.</summary>
	public required int PsirtId { get; init; }
}
