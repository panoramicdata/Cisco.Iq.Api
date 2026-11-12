using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
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
