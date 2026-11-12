using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
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
}
