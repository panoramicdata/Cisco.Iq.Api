using Cisco.Iq.Api.Generator;

var specDirectory = Argument("--spec-dir") ?? "docs/ga-spec";
var outputDirectory = Argument("--out") ?? "Cisco.Iq.Api/V1/Generated";
var keys = (Argument("--apis") ?? string.Join(",", ApiDefinition.All.Select(api => api.Key))).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
var check = args.Contains("--check");

var apis = keys.Select(key => ApiDefinition.All.FirstOrDefault(api => api.Key == key)
	?? throw new ArgumentException($"Unknown API '{key}'. Known: {string.Join(", ", ApiDefinition.All.Select(api => api.Key))}.")).ToList();
var result = ClientGenerator.Generate(specDirectory, apis);

foreach (var line in result.Report)
{
	Console.WriteLine(line);
}

foreach (var count in result.OperationCounts)
{
	Console.WriteLine($"{count.Key}: {count.Value} operations in the spec");
}

if (check)
{
	var drift = result.Files.Where(file => !File.Exists(Path.Combine(outputDirectory, file.Key))
		|| File.ReadAllText(Path.Combine(outputDirectory, file.Key)).Replace("\r\n", "\n") != file.Value).Select(file => file.Key).ToList();
	if (drift.Count > 0)
	{
		Console.Error.WriteLine($"Generated code is out of date: {string.Join(", ", drift)}. Run the generator without --check.");
		return 1;
	}

	Console.WriteLine("Generated code is up to date.");
	return 0;
}

Directory.CreateDirectory(outputDirectory);
foreach (var file in result.Files)
{
	File.WriteAllText(Path.Combine(outputDirectory, file.Key), file.Value);
	Console.WriteLine($"wrote {file.Key}");
}

return 0;

string? Argument(string name)
{
	var index = Array.IndexOf(args, name);
	return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
}
