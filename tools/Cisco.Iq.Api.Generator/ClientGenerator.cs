namespace Cisco.Iq.Api.Generator;

/// <summary>Generates the Cisco IQ GA client sources from the OpenAPI files.</summary>
public static class ClientGenerator
{
	/// <summary>Generates models, request objects, Refit interfaces and paging helpers for the given APIs.</summary>
	/// <param name="specDirectory">The directory holding the OpenAPI files.</param>
	/// <param name="apis">The APIs to generate.</param>
	public static GenerationResult Generate(string specDirectory, IEnumerable<ApiDefinition> apis)
	{
		var files = new SortedDictionary<string, string>(StringComparer.Ordinal);
		var report = new List<string>();
		var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
		foreach (var api in apis)
		{
			// nosemgrep: csharp.lang.security.filesystem.unsafe-path-combine.unsafe-path-combine -- a developer tool reading its own command-line directory; FileName is a constant.
			var yaml = File.ReadAllText(Path.Combine(specDirectory, api.FileName));
			var generator = new ApiGenerator(api, Node.Parse(yaml));
			var output = generator.Generate();
			files[$"{api.Namespace}.Models.g.cs"] = output.Models;
			files[$"{api.Namespace}.Api.g.cs"] = output.Api;
			report.AddRange(output.Report.Select(line => $"[{api.Key}] {line}"));
			counts[api.Key] = output.OperationCount;
		}

		return new GenerationResult(files, report, counts);
	}
}
