using Cisco.Iq.Api.Data;

namespace Cisco.Iq.Api;

/// <summary>Parameters for GetAssetLifecycleAsync.</summary>
public sealed record GetAssetLifecycleRequest : IRequest<AssetLifecycle>
{
	/// <summary>The assetId for this request.</summary>
	public required string AssetId { get; init; }
	/// <summary>The milestoneType for this request.</summary>
	public CiscoIqMilestoneType MilestoneType { get; init; }
}
