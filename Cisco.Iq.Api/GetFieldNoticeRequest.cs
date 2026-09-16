using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetFieldNoticeAsync.</summary>
public sealed record GetFieldNoticeRequest : IRequest<FieldNotice>
{
	/// <summary>The fieldNoticeId for this request.</summary>
	public required int FieldNoticeId { get; init; }
}
