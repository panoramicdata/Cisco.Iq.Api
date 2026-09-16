using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Cisco.Iq.Api;

/// <summary>The collection sort direction.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum CiscoIqSortOrder
{
	/// <summary>Ascending order.</summary>
	[EnumMember(Value = "ASC")]
	Ascending = 0,
	/// <summary>Descending order.</summary>
	[EnumMember(Value = "DESC")]
	Descending = 1
}
