using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
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
}
