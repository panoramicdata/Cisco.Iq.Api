namespace Cisco.Iq.Api.V1.Http;

internal static class CiscoIqV1RequestCopy
{
	internal static async Task<HttpRequestMessage> CreateAsync(HttpRequestMessage original, CancellationToken cancellationToken)
	{
		var copy = new HttpRequestMessage(original.Method, original.RequestUri)
		{
			Version = original.Version,
			VersionPolicy = original.VersionPolicy
		};
		foreach (var header in original.Headers)
		{
			copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
		}

		foreach (var option in original.Options)
		{
			copy.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
		}

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
