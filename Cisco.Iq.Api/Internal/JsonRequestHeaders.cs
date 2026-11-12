using System.Net.Http.Headers;

namespace Cisco.Iq.Api.Internal;

internal static class JsonRequestHeaders
{
	internal static void Apply(HttpRequestMessage request, string userAgent)
	{
		request.Headers.Accept.Clear();
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		request.Headers.UserAgent.Clear();
		request.Headers.UserAgent.ParseAdd(userAgent);
	}
}
