using System.Globalization;
using System.Reflection;
using Refit;

namespace Cisco.Iq.Api;

/// <summary>Formats Cisco IQ query values using the API's wire conventions.</summary>
public sealed class CiscoIqUrlParameterFormatter : IUrlParameterFormatter
{
	/// <inheritdoc />
	public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
		=> value switch
		{
			null => null,
			DateTimeOffset date => date.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture),
			CiscoIqSortOrder.Ascending => "ASC",
			CiscoIqSortOrder.Descending => "DESC",
			CiscoIqMilestoneType.Hardware => "hardware",
			CiscoIqMilestoneType.Software => "software",
			CiscoIqAccountRegion.Us => "US",
			CiscoIqAccountRegion.Emea => "EMEA",
			CiscoIqAccountRegion.Apjc => "APJC",
			Enum => throw new ArgumentException("Undefined enum query value.", nameof(value)),
			bool boolean => boolean ? "true" : "false",
			_ => Convert.ToString(value, CultureInfo.InvariantCulture)
		};
}
