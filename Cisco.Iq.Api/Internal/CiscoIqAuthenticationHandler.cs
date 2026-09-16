using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace Cisco.Iq.Api.Internal;

internal sealed class CiscoIqAuthenticationHandler(CiscoIqClientOptions options, HttpClient exchangeClient, TimeProvider timeProvider) : DelegatingHandler
{
	private readonly SemaphoreSlim _refreshLock = new(1, 1);
	private TokenState? _token;
	private sealed record TokenState(string AccessToken, DateTimeOffset ExpiresAt);

	protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var token = await GetTokenAsync(null, cancellationToken).ConfigureAwait(false);
		Stamp(request, token.AccessToken);
		var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		if (response.StatusCode == HttpStatusCode.Unauthorized)
		{
			response.Dispose();
			token = await GetTokenAsync(token, cancellationToken).ConfigureAwait(false);
			using var retry = RequestCopy.Create(request);
			Stamp(retry, token.AccessToken);
			response = await base.SendAsync(retry, cancellationToken).ConfigureAwait(false);
		}
		if (!response.IsSuccessStatusCode)
		{
			using (response)
			{
				throw await CiscoIqApiException.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
			}
		}
		return response;
	}

	private void Stamp(HttpRequestMessage request, string accessToken)
	{
		request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
		request.Headers.Remove("Cookie");
		request.Headers.Add("Cookie", "account_region=" + options.AccountRegion.ToString().ToUpperInvariant());
		request.Headers.Accept.Clear();
		request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		request.Headers.UserAgent.Clear();
		request.Headers.UserAgent.ParseAdd(options.EffectiveUserAgent);
	}

	private bool IsValid(TokenState? token, TokenState? rejected)
		=> token is not null && !ReferenceEquals(token, rejected) && timeProvider.GetUtcNow() < token.ExpiresAt;

	private async Task<TokenState> GetTokenAsync(TokenState? rejected, CancellationToken cancellationToken)
	{
		var token = Volatile.Read(ref _token);
		if (IsValid(token, rejected))
		{
			return token!;
		}
		await _refreshLock.WaitAsync(cancellationToken).ConfigureAwait(false);
		try
		{
			token = Volatile.Read(ref _token);
			if (IsValid(token, rejected))
			{
				return token!;
			}
			using var request = new HttpRequestMessage(HttpMethod.Post, "https://iq.cisco.com/cxp-iam/api/v1/auth/issueToken");
			request.Headers.Authorization = new AuthenticationHeaderValue("Basic", options.Token);
			request.Headers.Add("Cookie", "account_region=" + options.AccountRegion.ToString().ToUpperInvariant());
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			request.Headers.UserAgent.ParseAdd(options.EffectiveUserAgent);
			var body = options.AccountId is null ? "{}" : JsonConvert.SerializeObject(new Dictionary<string, string> { ["accountId"] = options.AccountId });
			request.Content = new StringContent(body, Encoding.UTF8, "application/json");
			using var response = await exchangeClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
			if (!response.IsSuccessStatusCode)
			{
				throw await CiscoIqApiException.FromResponseAsync(response, cancellationToken).ConfigureAwait(false);
			}
			var payload = JsonConvert.DeserializeObject<TokenResponse>(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
			if (string.IsNullOrWhiteSpace(payload?.AccessToken) || payload.ExpiresInSeconds <= 0)
			{
				throw new CiscoIqAuthenticationException("Token exchange returned an invalid access token or expiry.", null, null);
			}
			token = new(payload.AccessToken, timeProvider.GetUtcNow().AddSeconds(payload.ExpiresInSeconds) - options.TokenRefreshMargin);
			Volatile.Write(ref _token, token);
			return token;
		}
		finally
		{
			_refreshLock.Release();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_refreshLock.Dispose();
		}
		base.Dispose(disposing);
	}

	private sealed class TokenResponse
	{
		[JsonProperty("accessToken")]
		public string? AccessToken { get; set; }
		[JsonProperty("expiresInSeconds")]
		public double ExpiresInSeconds { get; set; }
	}
}
