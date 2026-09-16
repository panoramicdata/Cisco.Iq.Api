using System.Net;

namespace Cisco.Iq.Api.Test;

internal sealed class TestTransport(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var response = await send(request, cancellationToken);
		response.RequestMessage = request;
		return response;
	}

	internal static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK)
		=> new(status) { Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json") };
}
