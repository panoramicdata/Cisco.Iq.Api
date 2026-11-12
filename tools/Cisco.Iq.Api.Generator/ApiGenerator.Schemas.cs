using System.Text;
using System.Text.RegularExpressions;

namespace Cisco.Iq.Api.Generator;

internal sealed partial class ApiGenerator
{
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
}
