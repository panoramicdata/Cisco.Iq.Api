using Newtonsoft.Json;

namespace Cisco.Iq.Api.Data;

/// <summary>Converts nullable timestamps to and from Unix epoch milliseconds.</summary>
public sealed class UnixMillisecondsConverter : JsonConverter<DateTimeOffset?>
{
	/// <inheritdoc />
	public override DateTimeOffset? ReadJson(JsonReader reader, Type objectType, DateTimeOffset? existingValue, bool hasExistingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}
		if (reader.TokenType != JsonToken.Integer)
		{
			throw new JsonSerializationException("Expected an integer Unix millisecond timestamp.");
		}
		return DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(reader.Value, System.Globalization.CultureInfo.InvariantCulture));
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, DateTimeOffset? value, JsonSerializer serializer)
	{
		if (value.HasValue)
		{
			writer.WriteValue(value.Value.ToUnixTimeMilliseconds());
		}
		else
		{
			writer.WriteNull();
		}
	}
}
