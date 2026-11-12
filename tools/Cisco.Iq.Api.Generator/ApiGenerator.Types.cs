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

	private void EmitSchema(string name)
	{
		var node = _schemas[name];
		var typeName = Naming.TypeName(name);
		if (_types.ContainsKey(typeName))
		{
			throw new InvalidOperationException($"Two schemas in {_api.Key} map to the type name {typeName}.");
		}

		if (IsEnum(node))
		{
			EmitEnum(typeName, node);
		}
		else
		{
			EmitClass(typeName, node, _bodies.Contains(name), _patches.Contains(name));
		}
	}

	private void EmitClass(string typeName, Node schema, bool isRequestBody = false, bool isPatch = false)
	{
		AddType(typeName, string.Empty);
		var builder = new StringBuilder();
		builder.AppendLine(Summary(schema["description"]?.Text ?? schema["title"]?.Text));
		builder.AppendLine(GeneratedCode());
		builder.Line($"public class {typeName}");
		builder.AppendLine("{");
		var used = new HashSet<string> { typeName };
		foreach (var property in schema["properties"]!.Entries)
		{
			EmitProperty(builder, used, typeName, property, isRequestBody, isPatch);
		}

		if (schema["properties"]!.Has("after") && isRequestBody)
		{
			EmitWithAfter(builder, typeName);
		}

		builder.AppendLine("}");
		_types[typeName] = builder.ToString();
	}

	private void EmitProperty(StringBuilder builder, HashSet<string> used, string typeName, KeyValuePair<string, Node> property, bool isRequestBody, bool isPatch)
	{
		var readOnly = property.Value["readOnly"]?.Text == "true";
		if (readOnly && isRequestBody)
		{
			return;
		}

		var type = ResolvePropertyType(typeName, property);
		WriteProperty(builder, property, UniqueName(used, property.Key), type, readOnly, isPatch);
	}

	private string ResolvePropertyType(string typeName, KeyValuePair<string, Node> property)
		=> property.Key == "_type" ? "string" : OverrideFor(typeName, property.Key)?.CSharp ?? MapType(property.Value, typeName + Naming.Pascal(property.Key));

	private static void WriteProperty(StringBuilder builder, KeyValuePair<string, Node> property, string name, string type, bool readOnly, bool isPatch)
	{
		builder.Line($"\t{Summary(property.Value["description"]?.Text ?? property.Value["title"]?.Text)}");
		builder.Line($"\t[JsonProperty(\"{property.Key}\")]");
		if (readOnly)
		{
			builder.AppendLine("\t[CiscoIqV1ReadOnly]");
		}

		var propertyType = isPatch ? $"CiscoIqV1Optional<{type}>" : $"{type}?";
		builder.Line($"\tpublic {propertyType} {name} {{ get; set; }}");
		builder.AppendLine();
	}

	private static string UniqueName(HashSet<string> used, string key)
	{
		var name = key == "_type" ? "ResourceType" : Naming.Pascal(key);
		while (!used.Add(name))
		{
			name += "Value";
		}

		return name;
	}

	private static void EmitWithAfter(StringBuilder builder, string typeName)
	{
		builder.AppendLine("\t/// <summary>Returns a copy with the paging cursor replaced.</summary>");
		builder.Line($"\tpublic {typeName} WithAfter(string? after)");
		builder.AppendLine("\t{");
		builder.Line($"\t\tvar copy = ({typeName})MemberwiseClone();");
		builder.AppendLine("\t\tcopy.After = after;");
		builder.AppendLine("\t\treturn copy;");
		builder.AppendLine("\t}");
	}

	private void EmitEnum(string typeName, Node schema)
	{
		var builder = new StringBuilder();
		builder.AppendLine(Summary(schema["description"]?.Text ?? schema["title"]?.Text));
		builder.AppendLine(GeneratedCode());
		builder.Line($"[JsonConverter(typeof(CiscoIqV1StringValueConverter<{typeName}>))]");
		builder.Line($"public readonly record struct {typeName}(string Value) : ICiscoIqV1StringValue<{typeName}>");
		builder.AppendLine("{");
		var used = new HashSet<string> { typeName, "Value", "Create" };
		foreach (var value in schema["enum"]!.Items.Select(item => item.Text!))
		{
			var member = Naming.EnumMember(value);
			while (!used.Add(member))
			{
				member += "Value";
			}

			builder.Line($"\t/// <summary>The value {Escape(value)}.</summary>");
			builder.Line($"\tpublic static readonly {typeName} {member} = new(\"{value}\");");
			builder.AppendLine();
		}

		builder.Line($"\t/// <inheritdoc />");
		builder.Line($"\tpublic static {typeName} Create(string value) => new(value);");
		builder.AppendLine();
		builder.AppendLine("\t/// <inheritdoc />");
		builder.AppendLine("\tpublic override string ToString() => Value;");
		builder.AppendLine("}");
		AddType(typeName, builder.ToString());
	}
}
