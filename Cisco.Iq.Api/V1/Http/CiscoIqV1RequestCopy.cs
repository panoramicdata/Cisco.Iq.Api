using Cisco.Iq.Api.Internal;

namespace Cisco.Iq.Api.V1.Http;

internal static class CiscoIqV1RequestCopy
{
	internal static async Task<HttpRequestMessage> CreateAsync(HttpRequestMessage original, CancellationToken cancellationToken)
	{
		var copy = RequestCopy.Create(original);
		if (original.Content is not null)
		{
			var bytes = await original.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
			copy.Content = new ByteArrayContent(bytes);
			foreach (var header in original.Content.Headers)
			{
				copy.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
			}
		}

		return copy;
	}
}
