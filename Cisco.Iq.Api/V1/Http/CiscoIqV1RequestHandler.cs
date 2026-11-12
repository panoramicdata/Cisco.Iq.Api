using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Cisco.Iq.Api.Internal;

namespace Cisco.Iq.Api.V1.Http;

internal sealed class CiscoIqV1RequestHandler(CiscoIqV1ClientOptions options) : DelegatingHandler
{
	internal const int MaxQueryBytes = 2048;

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var query = request.RequestUri!.Query.TrimStart('?');
		var length = Encoding.UTF8.GetByteCount(query);
		if (length > MaxQueryBytes)
		{
			throw new CiscoIqV1QueryTooLongException(length);
		}

		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
		JsonRequestHeaders.Apply(request, options.EffectiveUserAgent);
		request.Headers.Remove("X-IQ-Region");
		if (options.SendRegionHeader)
		{
			request.Headers.Add("X-IQ-Region", options.Region.ToWireValue());
		}

		if (!request.Headers.Contains("traceparent"))
		{
			request.Headers.Add("traceparent", NewTraceparent());
		}

		return base.SendAsync(request, cancellationToken);
	}

	internal static string NewTraceparent()
		=> $"00-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16))}-{Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(8))}-01";
}
