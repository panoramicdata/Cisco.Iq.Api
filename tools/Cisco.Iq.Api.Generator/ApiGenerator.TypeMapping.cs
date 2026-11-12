using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
	private string MapType(Node schema, string context)
	{
		if (schema["$ref"]?.Text is { } reference)
		{
			return schema.Has("enum") ? "string" : MapReference(reference[(reference.LastIndexOf('/') + 1)..]);
		}

		var alternatives = schema["anyOf"] is { IsList: true } anyOf ? anyOf : schema["oneOf"] is { IsList: true } oneOf ? oneOf : null;
		return alternatives is not null ? MapAlternatives(alternatives, context) : MapByType(schema, context);
	}

	private string MapAlternatives(Node alternatives, string context)
	{
		var concrete = alternatives.Items.Where(item => item["type"]?.Text != "null").ToList();
		return concrete.Count == 1 ? MapType(concrete[0], context) : Fallback(context);
	}

	private string MapByType(Node schema, string context)
		=> TypeNames(schema).FirstOrDefault() switch
		{
			"string" => schema["format"]?.Text switch { "date-time" => "DateTimeOffset", "date" => "DateOnly", _ => "string" },
			"integer" => schema["format"]?.Text == "int32" ? "int" : "long",
			"number" => "double",
			"boolean" => "bool",
			"array" => schema["items"] is { } items ? $"List<{MapType(items, context + "Item")}>" : "List<JToken>",
			"object" => MapObject(schema, context),
			_ => schema.Has("properties") ? MapObject(schema, context) : Fallback(context)
		};

	private string MapObject(Node schema, string context)
	{
		if (schema.Has("properties"))
		{
			if (!_types.ContainsKey(context))
			{
				EmitClass(context, schema);
			}

			return context;
		}

		var additional = schema["additionalProperties"];
		return additional is { IsMap: true } ? $"Dictionary<string, {MapType(additional, context + "Value")}>" : "Dictionary<string, JToken>";
	}

	private string MapReference(string name)
	{
		var target = _schemas[name];
		if (IsEnum(target) || target.Has("properties"))
		{
			if (_requested.Add(name))
			{
				_pending.Enqueue(name);
			}

			return Naming.TypeName(name);
		}

		return MapType(target, Naming.TypeName(name));
	}

	private string Fallback(string context)
	{
		if (_fallbacks.Add(context))
		{
			_report.Add($"{context}: no single type, mapped to JToken");
		}

		return "JToken";
	}

	private static bool IsEnum(Node schema) => schema["enum"] is { IsList: true } && TypeNames(schema).Contains("string");

	private static List<string> TypeNames(Node schema)
	{
		var type = schema["type"];
		var names = type is null ? [] : type.Text is { } single ? [single] : type.Items.Select(item => item.Text!).ToList();
		return names.Where(name => name != "null").ToList();
	}
}
