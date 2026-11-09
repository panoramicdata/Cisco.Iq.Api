using YamlDotNet.Serialization;

namespace Cisco.Iq.Api.Generator;

/// <summary>A read-only view over a parsed YAML value.</summary>
internal sealed class Node(object? value)
{
	internal static Node Parse(string yaml) => new(new DeserializerBuilder().Build().Deserialize<object>(yaml));

	internal Node? this[string key]
		=> value is Dictionary<object, object> map && map.TryGetValue(key, out var found) ? new Node(found) : null;

	internal string? Text => value as string;

	internal bool IsList => value is List<object>;

	internal bool IsMap => value is Dictionary<object, object>;

	internal IEnumerable<Node> Items => value is List<object> list ? list.Select(item => new Node(item)) : [];

	internal IEnumerable<KeyValuePair<string, Node>> Entries
		=> value is Dictionary<object, object> map ? map.Select(pair => KeyValuePair.Create((string)pair.Key, new Node(pair.Value))) : [];

	internal bool Has(string key) => this[key] is not null;
}
