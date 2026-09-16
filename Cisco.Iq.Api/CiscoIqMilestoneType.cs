using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cisco.Iq.Api;

/// <summary>The lifecycle milestone category.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum CiscoIqMilestoneType
{
	/// <summary>Hardware milestones.</summary>
	[EnumMember(Value = "hardware")]
	Hardware = 0,
	/// <summary>Software milestones.</summary>
	[EnumMember(Value = "software")]
	Software = 1
}
