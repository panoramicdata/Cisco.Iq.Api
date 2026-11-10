using System.Globalization;
using System.Text;

namespace Cisco.Iq.Api.Generator;

internal static class Extensions
{
	internal static StringBuilder Line(this StringBuilder builder, FormattableString text)
		=> builder.AppendLine(text.ToString(CultureInfo.InvariantCulture));
}
