using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cisco.Iq.Api;

/// <summary>
/// The Cisco IQ data storage region for an account.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum CiscoIqAccountRegion
{
	/// <summary>United States.</summary>
	[EnumMember(Value = "US")]
	Us = 0,

	/// <summary>Europe, Middle East and Africa.</summary>
	[EnumMember(Value = "EMEA")]
	Emea = 1,

	/// <summary>Asia Pacific, Japan and China.</summary>
	[EnumMember(Value = "APJC")]
	Apjc = 2
}
