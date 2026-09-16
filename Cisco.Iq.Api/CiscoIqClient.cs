using Cisco.Iq.Api.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;

namespace Cisco.Iq.Api;

/// <summary>A client for the Cisco IQ Assets and Assessments REST APIs.</summary>
public sealed partial class CiscoIqClient : IDisposable
{
	private readonly HttpClient _http;
	private readonly HttpClient _exchange;
	private CiscoIqRateLimitStatus? _lastRateLimitStatus;

	/// <summary>Creates a client with the supplied credentials and settings.</summary>
	public CiscoIqClient(CiscoIqClientOptions options, ILogger? logger = null)
		: this(options, CreateTransport(), CreateTransport(), logger: logger)
	{
	}

	internal CiscoIqClient(CiscoIqClientOptions options, HttpMessageHandler productTransport, HttpMessageHandler exchangeTransport,
		TimeProvider? timeProvider = null, Func<TimeSpan, CancellationToken, Task>? delay = null, ILogger? logger = null)
	{
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();
		// Snapshot mutable options so one client's account and token cannot change mid-request.
		var settings = new CiscoIqClientOptions
		{
			Token = options.Token,
			AccountId = options.AccountId,
			AccountRegion = options.AccountRegion,
			HttpClientTimeoutSeconds = options.HttpClientTimeoutSeconds,
			TokenRefreshMargin = options.TokenRefreshMargin,
			RetryRateLimitedRequests = options.RetryRateLimitedRequests,
			MaxAttemptCount = options.MaxAttemptCount,
			UserAgent = options.UserAgent
		};
		_exchange = new HttpClient(exchangeTransport) { Timeout = TimeSpan.FromSeconds(settings.HttpClientTimeoutSeconds) };
		var rateLimit = new CiscoIqRateLimitHandler(settings, status => Volatile.Write(ref _lastRateLimitStatus, status), delay ?? Task.Delay)
		{
			InnerHandler = productTransport
		};
		var authentication = new CiscoIqAuthenticationHandler(settings, _exchange, timeProvider ?? TimeProvider.System) { InnerHandler = rateLimit };
		_http = new HttpClient(authentication)
		{
			BaseAddress = new Uri("https://iq.cisco.com/ciq-rest/api/v0"),
			Timeout = TimeSpan.FromSeconds(settings.HttpClientTimeoutSeconds)
		};
		var refit = new RefitSettings
		{
			ContentSerializer = new NewtonsoftJsonContentSerializer(new JsonSerializerSettings()),
			UrlParameterFormatter = new CiscoIqUrlParameterFormatter(),
			CollectionFormat = CollectionFormat.Multi,
			TransportExceptionFactory = (_, exception, _) => exception
		};
		var reader = new PageReader(_http);
		Assets = new AssetsClient(RestService.For<IAssetsApi>(_http, refit), reader);
		Assessments = new AssessmentsClient(RestService.For<IAssessmentsApi>(_http, refit), reader);
		if (logger is not null)
		{
			LogInitialized(logger, settings.AccountRegion);
		}
	}

	/// <summary>The six Assets operations and their paging companions.</summary>
	public IAssets Assets { get; }
	/// <summary>The ten Assessments operations and their paging companions.</summary>
	public IAssessments Assessments { get; }
	/// <summary>The latest observed rate-limit headers, or null before any are received.</summary>
	public CiscoIqRateLimitStatus? LastRateLimitStatus => Volatile.Read(ref _lastRateLimitStatus);

	internal static HttpClientHandler CreateTransport() => new() { UseCookies = false, AllowAutoRedirect = false };

	[LoggerMessage(Level = LogLevel.Debug, Message = "Cisco IQ client initialized for region {Region}.")]
	private static partial void LogInitialized(ILogger logger, CiscoIqAccountRegion region);

	/// <inheritdoc />
	public void Dispose()
	{
		_http.Dispose();
		_exchange.Dispose();
	}
}
