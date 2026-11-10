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
	private readonly HashSet<string> _bodies = [];
	private readonly List<SpecOverride> _overrides;
	private readonly HashSet<string> _patches = [];

	internal ApiGenerator(ApiDefinition api, Node root, IReadOnlyList<SpecOverride> overrides)
	{
		_api = api;
		_overrides = overrides.Where(o => o.Api == api.Key).ToList();
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
				EmitOperation(serverPath + path.Key, method.Key, method.Value, path.Value["parameters"]?.Items.ToList() ?? []);
			}
		}

		while (_pending.Count > 0)
		{
			EmitSchema(_pending.Dequeue());
		}

		return new ApiOutput(ComposeModels(), ComposeApi(), _report, operations);
	}

	private void EmitOperation(string path, string verb, Node operation, List<Node> sharedParameters)
	{
		var baseName = Naming.Pascal(operation["operationId"]!.Text!);
		var methodName = baseName + "Async";
		if (!_methodNames.Add(methodName))
		{
			throw new InvalidOperationException($"Duplicate operation name {methodName} in {_api.Key}.");
		}

		var (pathParameters, headerParameters, query) = ReadParameters(operation, sharedParameters, baseName);
		var body = ReadBody(operation, baseName);
		var requestName = baseName + "Request";
		if (query.Count > 0)
		{
			EmitRequest(requestName, query);
		}

		var response = ResponseSchema(operation);
		var returnType = response is null ? "Task<IApiResponse>" : $"Task<IApiResponse<{MapType(response, baseName + "Response")}>>";
		var arguments = pathParameters.Select(p => $"{p.Alias}{p.Type} {p.Name}").ToList();
		if (body is not null)
		{
			arguments.Add(body.Value.Required ? $"[Body] {body.Value.Type} body" : $"[Body] {body.Value.Type}? body = null");
		}

		if (query.Count > 0)
		{
			arguments.Add($"[Query] {requestName}? request = null");
		}

		arguments.AddRange(headerParameters);
		arguments.Add("CancellationToken cancellationToken = default");
		_members.Line($"\t{Summary(operation["summary"]?.Text ?? operation["description"]?.Text)}");
		_members.Line($"\t[{Naming.Pascal(verb)}(\"{path}\")]");
		if (body is { MergePatch: true })
		{
			_members.Line($"\t[Headers(\"Content-Type: application/merge-patch+json\")]");
		}

		_members.Line($"\t{returnType} {methodName}({string.Join(", ", arguments)});");
		_members.AppendLine();
		EmitPaging(verb, operation, (methodName, baseName, pathParameters, requestName), query, body, response);
	}

	private void EmitPaging(string verb, Node operation, (string MethodName, string BaseName, List<(string Alias, string Type, string Name)> Path, string RequestName) names, List<(string Name, string Wire, string Type, bool Csv, string? Doc)> query, (string Type, bool Required, bool MergePatch, string? SchemaName)? body, Node? response)
	{
		if (response is null)
		{
			return;
		}

		if (verb == "get" && query.Any(q => q.Wire == "after") && ItemType(response) is { } itemType)
		{
			EmitPagingExtension(operation, names.MethodName, names.BaseName, names.Path, names.RequestName, query.Any(q => q.Wire == "count"), itemType);
		}
		else if (verb == "post" && BodyHasCursor(body) && ItemType(response) is { } bodyItemType)
		{
			EmitBodyPagingExtension(operation, names.MethodName, names.BaseName, body!.Value.Type, bodyItemType);
		}
	}

	private bool BodyHasCursor((string Type, bool Required, bool MergePatch, string? SchemaName)? body)
		=> body?.SchemaName is { } name && _schemas[name]["properties"]?.Has("after") == true;

	private (string Type, bool Required, bool MergePatch, string? SchemaName)? ReadBody(Node operation, string baseName)
	{
		var body = operation["requestBody"];
		if (body is null)
		{
			return null;
		}

		var content = body["content"]!.Entries.Single();
		var mergePatch = content.Key == "application/merge-patch+json";
		if (!mergePatch && content.Key != "application/json")
		{
			throw new InvalidOperationException($"Unsupported request content type '{content.Key}' on {baseName}.");
		}

		var schema = content.Value["schema"]!;
		var schemaName = schema["$ref"]?.Text is { } reference ? reference[(reference.LastIndexOf('/') + 1)..] : null;
		RegisterBody(schemaName, mergePatch);
		return (MapType(schema, baseName + "Body"), body["required"]?.Text == "true", mergePatch, schemaName);
	}

	private void RegisterBody(string? schemaName, bool mergePatch)
	{
		if (schemaName is null)
		{
			return;
		}

		_bodies.Add(schemaName);
		if (mergePatch)
		{
			_patches.Add(schemaName);
		}
	}

	private void EmitBodyPagingExtension(Node operation, string methodName, string baseName, string bodyType, string itemType)
	{
		_extensions.Line($"\t{Summary(operation["summary"]?.Text)}");
		_extensions.Line($"\tpublic static IAsyncEnumerable<{itemType}> {baseName}AllAsync(this {_api.InterfaceName} api, {bodyType} body, CancellationToken cancellationToken = default)");
		_extensions.Line($"\t\t=> CiscoIqV1Paging.EnumerateByBodyAsync(cursor => api.{methodName}(body.WithAfter(cursor), cancellationToken: cancellationToken), page => page.Items, page => page.After, cancellationToken);");
		_extensions.AppendLine();
	}

	private (List<(string Alias, string Type, string Name)> Path, List<string> Headers, List<(string Name, string Wire, string Type, bool Csv, string? Doc)> Query) ReadParameters(Node operation, List<Node> sharedParameters, string baseName)
	{
		var path = new List<(string Alias, string Type, string Name)>();
		var headers = new List<string>();
		var query = new List<(string Name, string Wire, string Type, bool Csv, string? Doc)>();
		foreach (var parameter in DistinctParameters(sharedParameters, operation))
		{
			var wire = parameter["name"]!.Text!;
			var location = parameter["in"]!.Text;
			if (location == "path")
			{
				path.Add(PathParameter(wire, parameter["schema"]!, baseName));
			}
			else if (location == "query")
			{
				query.Add(QueryParameter(parameter, wire, baseName));
			}
			else if (location != "header")
			{
				throw new InvalidOperationException($"Unsupported parameter location '{location}' on {baseName}.");
			}
			else if (!wire.Equals("traceparent", StringComparison.OrdinalIgnoreCase))
			{
				headers.Add($"[Header(\"{wire}\")] string? {Naming.Camel(wire)} = null");
			}
		}

		return (path, headers, query);
	}

	private IEnumerable<Node> DistinctParameters(List<Node> sharedParameters, Node operation)
		=> sharedParameters.Concat(operation["parameters"]?.Items ?? [])
			.Select(Resolve)
			.GroupBy(parameter => parameter["in"]!.Text + ":" + parameter["name"]!.Text)
			.Select(group => group.Last());

	private (string Alias, string Type, string Name) PathParameter(string wire, Node schema, string baseName)
	{
		var argument = Naming.Camel(wire);
		var alias = argument.TrimStart('@') == wire ? string.Empty : $"[AliasAs(\"{wire}\")] ";
		return (alias, MapType(schema, baseName + Naming.Pascal(wire)), argument);
	}

	private (string Name, string Wire, string Type, bool Csv, string? Doc) QueryParameter(Node parameter, string wire, string baseName)
	{
		var schema = parameter["schema"]!;
		var isList = TypeNames(schema).Contains("array");
		var doc = parameter["summary"]?.Text ?? parameter["description"]?.Text;
		if (isList)
		{
			doc = $"{doc}{KnownValues(schema["items"])}";
		}

		return (wire == "orderby" ? "OrderBy" : Naming.Pascal(wire), wire, isList ? "List<string>" : MapType(schema, baseName + Naming.Pascal(wire)), isList, doc);
	}

	private string KnownValues(Node? items)
	{
		var target = items?["$ref"]?.Text is { } reference ? _schemas[reference[(reference.LastIndexOf('/') + 1)..]] : items;
		return target is not null && IsEnum(target) ? $" (known values: {string.Join(", ", target["enum"]!.Items.Select(item => item.Text))})" : string.Empty;
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
		var success = operation["responses"]?.Entries.Where(entry => entry.Key.StartsWith('2')).OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => entry.Value).FirstOrDefault(entry => HasJson(entry));
		if (success?["$ref"]?.Text is { } reference)
		{
			success = _responses[reference[(reference.LastIndexOf('/') + 1)..]];
		}

		return success?["content"]?["application/json"]?["schema"];
	}

	private bool HasJson(Node response)
		=> (response["$ref"]?.Text is { } reference ? _responses[reference[(reference.LastIndexOf('/') + 1)..]] : response)["content"]?.Has("application/json") == true;

	private string? ItemType(Node response)
	{
		if (response["$ref"]?.Text is not { } reference)
		{
			return null;
		}

		var name = reference[(reference.LastIndexOf('/') + 1)..];
		if (OverrideFor(Naming.TypeName(name), "items") is { } replacement)
		{
			return replacement.CSharp[5..^1];
		}

		var items = _schemas[name]["properties"]?["items"];
		return items is not null && TypeNames(items).Contains("array") && items["items"] is { } element
			? MapType(element, Naming.TypeName(name) + "ItemsItem")
			: null;
	}

	private SpecOverride? OverrideFor(string typeName, string property)
	{
		var found = _overrides.FirstOrDefault(o => o.Type == typeName && o.Property == property);
		if (found is not null && _fallbacks.Add($"override:{typeName}.{property}"))
		{
			_report.Add($"{typeName}.{property}: overridden to {found.CSharp} (see docs/ga-spec/overrides.json)");
		}

		return found;
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

		var name = UniqueName(used, property.Key);
		var type = property.Key == "_type" ? "string" : OverrideFor(typeName, property.Key)?.CSharp ?? MapType(property.Value, typeName + Naming.Pascal(property.Key));
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
