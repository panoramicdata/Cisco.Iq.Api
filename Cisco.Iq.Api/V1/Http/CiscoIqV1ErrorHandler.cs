namespace Cisco.Iq.Api.V1.Http;

internal sealed class CiscoIqV1ErrorHandler : DelegatingHandler
{
	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		if (response.IsSuccessStatusCode)
		{
			return response;
		}

		try
		{
			throw await CiscoIqV1ApiException.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
		}
		finally
		{
			response.Dispose();
		}
	}
}
