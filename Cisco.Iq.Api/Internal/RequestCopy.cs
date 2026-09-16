namespace Cisco.Iq.Api.Internal;

internal static class RequestCopy
{
	internal static HttpRequestMessage Create(HttpRequestMessage original)
	{
		// All product operations are GET and have no body.
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
		return copy;
	}
}
