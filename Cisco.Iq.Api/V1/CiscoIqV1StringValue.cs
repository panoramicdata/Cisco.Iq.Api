using Newtonsoft.Json;

namespace Cisco.Iq.Api.V1;

/// <summary>A string-backed value that keeps values Cisco adds later, instead of failing to parse them.</summary>
/// <typeparam name="TSelf">The implementing type.</typeparam>
public interface ICiscoIqV1StringValue<TSelf> where TSelf : ICiscoIqV1StringValue<TSelf>
{
	/// <summary>The wire value.</summary>
	string Value { get; }

	/// <summary>Creates a value from its wire form.</summary>
	static abstract TSelf Create(string value);
}

/// <summary>Reads and writes a string-backed value, or its nullable form, as a plain JSON string.</summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class CiscoIqV1StringValueConverter<T> : JsonConverter where T : struct, ICiscoIqV1StringValue<T>
{
	/// <inheritdoc />
	public override bool CanConvert(Type objectType) => (Nullable.GetUnderlyingType(objectType) ?? objectType) == typeof(T);

	/// <inheritdoc />
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return Nullable.GetUnderlyingType(objectType) is null ? default(T) : null;
		}

		return reader.Value is string text
			? T.Create(text)
			: throw new JsonSerializationException($"Expected a string for {typeof(T).Name} but found {reader.TokenType}.");
	}

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		writer.WriteValue(((T)value!).Value);
	}
}
