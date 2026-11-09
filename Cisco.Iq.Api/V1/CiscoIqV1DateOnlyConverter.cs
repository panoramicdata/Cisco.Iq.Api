using System.Globalization;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.V1;

/// <summary>Reads and writes a date-only value, or its nullable form, as yyyy-MM-dd.</summary>
public sealed class CiscoIqV1DateOnlyConverter : JsonConverter
{
	private const string Format = "yyyy-MM-dd";

	/// <inheritdoc />
	public override bool CanConvert(Type objectType) => (Nullable.GetUnderlyingType(objectType) ?? objectType) == typeof(DateOnly);

	/// <inheritdoc />
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		return reader.Value switch
		{
			null when Nullable.GetUnderlyingType(objectType) is not null => null,
			null => default(DateOnly),
			string text => DateOnly.ParseExact(text, Format, CultureInfo.InvariantCulture),
			DateTime date => DateOnly.FromDateTime(date),
			_ => throw new JsonSerializationException($"Expected a date string but found {reader.TokenType}.")
		};
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		writer.WriteValue(((DateOnly)value!).ToString(Format, CultureInfo.InvariantCulture));
	}
}
