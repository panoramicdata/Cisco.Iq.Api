using System.Globalization;
using System.Reflection;
using Refit;

namespace Cisco.Iq.Api.V1;

/// <summary>Formats Cisco IQ GA query values using the API's wire conventions.</summary>
public sealed class CiscoIqV1UrlParameterFormatter : IUrlParameterFormatter
{
	/// <inheritdoc />
	public string? Format(object? value, ICustomAttributeProvider attributeProvider, Type type)
		=> value switch
		{
			null => null,
			bool boolean => boolean ? "true" : "false",
			DateTimeOffset date => date.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture),
			DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
			Enum => throw new ArgumentException("Undefined enum query value.", nameof(value)),
			IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
			_ => value.ToString()
		};
}
