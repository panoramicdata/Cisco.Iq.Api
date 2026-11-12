using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
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
}
