using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cisco.Iq.Api.V1;

/// <summary>Whether an optional JSON Merge Patch member is left out, set to a value, or set to null.</summary>
public enum CiscoIqV1OptionalState
{
	/// <summary>The member is not sent, so the server leaves it as it is.</summary>
	Unset = 0,

	/// <summary>The member is sent with its value.</summary>
	Value = 1,

	/// <summary>The member is sent as null, which clears it on the server.</summary>
	Null = 2
}

/// <summary>The non-generic view of an optional member, used when serialising.</summary>
public interface ICiscoIqV1Optional
{
	/// <summary>Whether the member is sent at all.</summary>
	bool IsSet { get; }

	/// <summary>Whether the member is sent as null.</summary>
	bool IsNull { get; }

	/// <summary>The value, or null.</summary>
	object? BoxedValue { get; }
}

/// <summary>A member of a JSON Merge Patch body, which can be left out, set, or cleared with null.</summary>
/// <typeparam name="T">The member type.</typeparam>
/// <param name="Value">The value to send.</param>
/// <param name="State">Whether the member is unset, a value, or null.</param>
public readonly record struct CiscoIqV1Optional<T>(T? Value, CiscoIqV1OptionalState State) : ICiscoIqV1Optional
{
	/// <inheritdoc />
	public bool IsSet => State != CiscoIqV1OptionalState.Unset;

	/// <inheritdoc />
	public bool IsNull => State == CiscoIqV1OptionalState.Null;

	/// <inheritdoc />
	public object? BoxedValue => State == CiscoIqV1OptionalState.Value ? Value : null;

	/// <summary>Converts a value to a member that is sent with that value.</summary>
	public static implicit operator CiscoIqV1Optional<T>(T value) => new(value, CiscoIqV1OptionalState.Value);
}

/// <summary>Creates optional Merge Patch members.</summary>
public static class CiscoIqV1Optional
{
	/// <summary>A member that is sent with the given value.</summary>
	public static CiscoIqV1Optional<T> Of<T>(T value) => new(value, CiscoIqV1OptionalState.Value);

	/// <summary>A member that is sent as null, which clears it on the server.</summary>
	public static CiscoIqV1Optional<T> Null<T>() => new(default, CiscoIqV1OptionalState.Null);
}

/// <summary>Writes an optional member as its value or as null. Unset members are skipped by the contract resolver.</summary>
public sealed class CiscoIqV1OptionalConverter : JsonConverter
{
	/// <inheritdoc />
	public override bool CanRead => false;

	/// <inheritdoc />
	public override bool CanConvert(Type objectType) => objectType.IsGenericType && objectType.GetGenericTypeDefinition() == typeof(CiscoIqV1Optional<>);

	/// <inheritdoc />
	public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		=> throw new NotSupportedException("Optional members are request-only.");

	/// <inheritdoc />
	public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
	{
		var optional = (ICiscoIqV1Optional)value!;
		if (optional.IsNull)
		{
			writer.WriteNull();
		}
		else
		{
			serializer.Serialize(writer, optional.BoxedValue);
		}
	}
}

/// <summary>Marks a property Cisco returns but never accepts, so the client never sends it back.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CiscoIqV1ReadOnlyAttribute : Attribute;

/// <summary>Leaves out read-only properties and unset Merge Patch members when a request body is written.</summary>
public sealed class CiscoIqV1ContractResolver : DefaultContractResolver
{
	/// <inheritdoc />
	protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
	{
		var property = base.CreateProperty(member, memberSerialization);
		if (member.GetCustomAttribute<CiscoIqV1ReadOnlyAttribute>() is not null)
		{
			property.ShouldSerialize = _ => false;
		}
		else if (property.PropertyType is { IsGenericType: true } type && type.GetGenericTypeDefinition() == typeof(CiscoIqV1Optional<>))
		{
			property.ShouldSerialize = instance => ((ICiscoIqV1Optional)property.ValueProvider!.GetValue(instance)!).IsSet;
		}

		return property;
	}
}
