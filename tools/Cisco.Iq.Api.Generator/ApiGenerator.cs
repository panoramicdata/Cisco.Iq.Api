using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed record ApiOutput(string Models, string Api, IReadOnlyList<string> Report, int OperationCount);

internal sealed partial class ApiGenerator
{
	private const string GeneratorName = "Cisco.Iq.Api.Generator";
	private const string GeneratorVersion = "1.0";
	private static readonly string[] HttpMethods = ["get", "put", "post", "delete", "options", "head", "patch", "trace"];

	private readonly ApiDefinition _api;
	private readonly Node _root;
	private readonly Dictionary<string, Node> _schemas;
	private readonly Dictionary<string, Node> _parameters;
	private readonly Dictionary<string, Node> _responses;
	private readonly SortedDictionary<string, string> _types = new(StringComparer.Ordinal);
	private readonly Queue<string> _pending = new();
	private readonly HashSet<string> _requested = [];
	private readonly HashSet<string> _fallbacks = [];
	private readonly List<string> _report = [];
	private readonly StringBuilder _members = new();
	private readonly StringBuilder _extensions = new();
	private readonly HashSet<string> _methodNames = [];

	internal ApiGenerator(ApiDefinition api, Node root)
	{
		_api = api;
		_root = root;
		_schemas = Entries(root["components"]?["schemas"]);
		_parameters = Entries(root["components"]?["parameters"]);
		_responses = Entries(root["components"]?["responses"]);
	}

	private static Dictionary<string, Node> Entries(Node? node)
		=> node is null ? [] : node.Entries.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);

	internal ApiOutput Generate()
	{
		var serverPath = new Uri(_root["servers"]!.Items.First()["url"]!.Text!).AbsolutePath.TrimEnd('/');
		var operations = 0;
		foreach (var path in _root["paths"]!.Entries)
		{
			foreach (var method in path.Value.Entries.Where(entry => HttpMethods.Contains(entry.Key)))
			{
				operations++;
				if (method.Key != "get")
				{
					_report.Add($"skipped {method.Key.ToUpperInvariant()} {path.Key} ({method.Value["operationId"]?.Text}): write operations are not generated yet");
					continue;
				}

				EmitOperation(serverPath + path.Key, method.Value);
			}
		}

		while (_pending.Count > 0)
		{
			EmitSchema(_pending.Dequeue());
		}

		return new ApiOutput(ComposeModels(), ComposeApi(), _report, operations);
	}

	private void EmitOperation(string path, Node operation)
	{
		var baseName = Naming.Pascal(operation["operationId"]!.Text!);
		var methodName = baseName + "Async";
		if (!_methodNames.Add(methodName))
		{
			throw new InvalidOperationException($"Duplicate operation name {methodName} in {_api.Key}.");
		}

		var pathParameters = new List<(string Alias, string Type, string Name)>();
		var headerParameters = new List<string>();
		var query = new List<(string Name, string Wire, string Type, bool Csv, string? Doc)>();
		foreach (var parameter in (operation["parameters"]?.Items ?? []).Select(Resolve))
		{
			var wire = parameter["name"]!.Text!;
			var schema = parameter["schema"]!;
			switch (parameter["in"]!.Text)
			{
				case "path":
					var argument = Naming.Camel(wire);
					var alias = argument.TrimStart('@') == wire ? string.Empty : $"[AliasAs(\"{wire}\")] ";
					pathParameters.Add((alias, MapType(schema, baseName + Naming.Pascal(wire)), argument));
					break;
				case "header" when !wire.Equals("traceparent", StringComparison.OrdinalIgnoreCase):
					headerParameters.Add($"[Header(\"{wire}\")] string? {Naming.Camel(wire)} = null");
					break;
				case "query":
					query.Add((wire == "orderby" ? "OrderBy" : Naming.Pascal(wire), wire, MapType(schema, baseName + Naming.Pascal(wire)), TypeNames(schema).Contains("array"), parameter["summary"]?.Text ?? parameter["description"]?.Text));
					break;
			}
		}

		var requestName = baseName + "Request";
		if (query.Count > 0)
		{
			EmitRequest(requestName, query);
		}

		var response = ResponseSchema(operation);
		var returnType = response is null ? "Task<IApiResponse>" : $"Task<IApiResponse<{MapType(response, baseName + "Response")}>>";
		var arguments = pathParameters.Select(p => $"{p.Alias}{p.Type} {p.Name}").ToList();
		if (query.Count > 0)
		{
			arguments.Add($"[Query] {requestName}? request = null");
		}

		arguments.AddRange(headerParameters);
		arguments.Add("CancellationToken cancellationToken = default");
		_members.Line($"\t{Summary(operation["summary"]?.Text ?? operation["description"]?.Text)}");
		_members.Line($"\t[Get(\"{path}\")]");
		_members.Line($"\t{returnType} {methodName}({string.Join(", ", arguments)});");
		_members.AppendLine();
		if (query.Any(q => q.Wire == "after") && response is not null && ItemType(response) is { } itemType)
		{
			EmitPagingExtension(operation, methodName, baseName, pathParameters, requestName, query.Any(q => q.Wire == "count"), itemType);
		}
	}

	private void EmitPagingExtension(Node operation, string methodName, string baseName, List<(string Alias, string Type, string Name)> pathParameters, string requestName, bool hasCount, string itemType)
	{
		var parameters = pathParameters.Select(p => $"{p.Type} {p.Name}").ToList();
		parameters.Add($"{requestName}? request = null");
		var callArguments = pathParameters.Select(p => p.Name).ToList();
		callArguments.Add($"(request ?? new()) with {{ After = cursor{(hasCount ? ", Count = null" : string.Empty)} }}");
		_extensions.Line($"\t{Summary(operation["summary"]?.Text)}");
		_extensions.Line($"\tpublic static IAsyncEnumerable<{itemType}> {baseName}AllAsync(this {_api.InterfaceName} api, {string.Join(", ", parameters)}, CancellationToken cancellationToken = default)");
		_extensions.Line($"\t\t=> CiscoIqV1Paging.EnumerateAsync(cursor => api.{methodName}({string.Join(", ", callArguments)}, cancellationToken: cancellationToken), page => page.Items, cancellationToken);");
		_extensions.AppendLine();
	}

	private Node? ResponseSchema(Node operation)
	{
		var ok = operation["responses"]?["200"];
		if (ok?["$ref"]?.Text is { } reference)
		{
			ok = _responses[reference[(reference.LastIndexOf('/') + 1)..]];
		}

		return ok?["content"]?["application/json"]?["schema"];
	}

	private string? ItemType(Node response)
	{
		if (response["$ref"]?.Text is not { } reference)
		{
			return null;
		}

		var name = reference[(reference.LastIndexOf('/') + 1)..];
		var items = _schemas[name]["properties"]?["items"];
		return items is not null && TypeNames(items).Contains("array") && items["items"] is { } element
			? MapType(element, Naming.TypeName(name) + "Item")
			: null;
	}

	private void EmitRequest(string name, List<(string Name, string Wire, string Type, bool Csv, string? Doc)> query)
	{
		var builder = new StringBuilder();
		builder.Line($"/// <summary>The query options for {name[..^"Request".Length]}.</summary>");
		builder.AppendLine(GeneratedCode());
		builder.Line($"public sealed record {name}");
		builder.AppendLine("{");
		foreach (var property in query)
		{
			builder.Line($"\t{Summary(property.Doc)}");
			builder.Line($"\t[AliasAs(\"{property.Wire}\")]");
			if (property.Csv)
			{
				builder.AppendLine("\t[Query(CollectionFormat.Csv)]");
			}

			builder.Line($"\tpublic {property.Type}? {property.Name} {{ get; init; }}");
			builder.AppendLine();
		}

		builder.AppendLine("}");
		AddType(name, builder.ToString());
	}

	private string MapType(Node schema, string context)
	{
		if (schema["$ref"]?.Text is { } reference)
		{
			return schema.Has("enum") ? "string" : MapReference(reference[(reference.LastIndexOf('/') + 1)..]);
		}

		foreach (var key in new[] { "anyOf", "oneOf" })
		{
			if (schema[key] is { IsList: true } alternatives)
			{
				var concrete = alternatives.Items.Where(item => item["type"]?.Text != "null").ToList();
				return concrete.Count == 1 ? MapType(concrete[0], context) : Fallback(context);
			}
		}

		var type = TypeNames(schema).FirstOrDefault();
		return type switch
		{
			"string" => schema["format"]?.Text switch { "date-time" => "DateTimeOffset", "date" => "DateOnly", _ => "string" },
			"integer" => schema["format"]?.Text == "int32" ? "int" : "long",
			"number" => "double",
			"boolean" => "bool",
			"array" => schema["items"] is { } items ? $"List<{MapType(items, context + "Item")}>" : "List<JToken>",
			"object" => MapObject(schema, context),
			_ => schema.Has("properties") ? MapObject(schema, context) : Fallback(context)
		};
	}

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
			EmitClass(typeName, node);
		}
	}

	private void EmitClass(string typeName, Node schema)
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
			var name = property.Key == "_type" ? "ResourceType" : Naming.Pascal(property.Key);
			while (!used.Add(name))
			{
				name += "Value";
			}

			var type = property.Key == "_type" ? "string" : MapType(property.Value, typeName + Naming.Pascal(property.Key));
			builder.Line($"\t{Summary(property.Value["description"]?.Text ?? property.Value["title"]?.Text)}");
			builder.Line($"\t[JsonProperty(\"{property.Key}\")]");
			builder.Line($"\tpublic {type}? {name} {{ get; set; }}");
			builder.AppendLine();
		}

		builder.AppendLine("}");
		_types[typeName] = builder.ToString();
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

	private void AddType(string name, string source)
	{
		if (source.Length > 0 && !_types.TryAdd(name, source))
		{
			throw new InvalidOperationException($"The type name {name} is generated twice in {_api.Key}.");
		}

		if (source.Length == 0)
		{
			_types[name] = string.Empty;
		}
	}

	private string ComposeModels()
	{
		var builder = new StringBuilder(Header());
		builder.Line($"namespace Cisco.Iq.Api.V1.{_api.Namespace};");
		builder.AppendLine();
		builder.AppendLine(string.Join("\n\n", _types.Values.Select(source => source.TrimEnd())));
		return Normalize(builder.ToString());
	}

	private string ComposeApi()
	{
		var builder = new StringBuilder(Header());
		builder.Line($"namespace Cisco.Iq.Api.V1.{_api.Namespace};");
		builder.AppendLine();
		builder.Line($"/// <summary>The Cisco IQ {_api.Key} API operations.</summary>");
		builder.AppendLine(GeneratedCode());
		builder.Line($"public interface {_api.InterfaceName}");
		builder.AppendLine("{");
		builder.Append(_members.ToString().TrimEnd());
		builder.AppendLine();
		builder.AppendLine("}");
		if (_extensions.Length > 0)
		{
			builder.AppendLine();
			builder.Line($"/// <summary>Paging helpers for the {_api.Key} API.</summary>");
			builder.AppendLine(GeneratedCode());
			builder.Line($"public static class {_api.InterfaceName[1..]}Extensions");
			builder.AppendLine("{");
			builder.Append(_extensions.ToString().TrimEnd());
			builder.AppendLine();
			builder.AppendLine("}");
		}

		return Normalize(builder.ToString());
	}

	private string Header()
		=> $"""
			// <auto-generated>
			// Generated by {GeneratorName} from {_api.FileName} (spec version {_root["info"]?["version"]?.Text}). Do not edit.
			// </auto-generated>
			#nullable enable
			#pragma warning disable CS1591
			using System.CodeDom.Compiler;
			using Cisco.Iq.Api.V1;
			using Newtonsoft.Json;
			using Newtonsoft.Json.Linq;
			using Refit;

			""".Replace("\r\n", "\n") + "\n";

	private static string GeneratedCode() => $"[GeneratedCode(\"{GeneratorName}\", \"{GeneratorVersion}\")]";

	private static string Normalize(string source) => source.Replace("\r\n", "\n").TrimEnd() + "\n";

	private Node Resolve(Node parameter)
		=> parameter["$ref"]?.Text is { } reference ? _parameters[reference[(reference.LastIndexOf('/') + 1)..]] : parameter;

	private static string Summary(string? text)
		=> $"/// <summary>{Escape(text is null ? "Generated from the Cisco IQ OpenAPI specification." : FirstSentence(text))}</summary>";

	private static string FirstSentence(string text)
	{
		var plain = MarkdownLink().Replace(text, "$1").Replace("`", string.Empty);
		plain = Whitespace().Replace(plain, " ").Trim();
		var end = plain.IndexOf(". ", StringComparison.Ordinal);
		if (end > 0)
		{
			plain = plain[..(end + 1)];
		}

		return plain.Length > 220 ? plain[..217] + "..." : plain;
	}

	private static string Escape(string text) => text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

	[GeneratedRegex(@"\[([^\]]*)\]\([^)]*\)")]
	private static partial Regex MarkdownLink();

	[GeneratedRegex(@"\s+")]
	private static partial Regex Whitespace();
}
