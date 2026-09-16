using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetFieldNoticesAsync.</summary>
public sealed record GetFieldNoticesRequest : IRequest<CiscoIqPage<FieldNotice>>
{
	/// <summary>The filter for this request.</summary>
	public FieldNoticeFilter? Filter { get; init; }
}
