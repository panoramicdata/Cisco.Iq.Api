using Cisco.Iq.Api.V1.Assessments;
using Cisco.Iq.Api.V1.Assets;
using Cisco.Iq.Api.V1.Http;
using Cisco.Iq.Api.V1.Identity;
using Cisco.Iq.Api.V1.Platform;
using Cisco.Iq.Api.V1.Products;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Refit;

namespace Cisco.Iq.Api.V1;

/// <summary>A client for the Cisco IQ GA REST APIs.</summary>
public sealed partial class CiscoIqV1Client : IDisposable
{
	private readonly HttpClient _http;
	private CiscoIqV1RateLimitInfo? _lastRateLimit;

	/// <summary>Creates a client with the supplied key and settings.</summary>
	public CiscoIqV1Client(CiscoIqV1ClientOptions options) : this(options, null)
	{
	}

	/// <summary>Creates a client with the supplied key and settings.</summary>
	public CiscoIqV1Client(CiscoIqV1ClientOptions options, ILogger? logger)
		: this(options, CreateTransport(), null, logger)
	{
	}

	internal CiscoIqV1Client(CiscoIqV1ClientOptions options, HttpMessageHandler transport, Func<TimeSpan, CancellationToken, Task>? delay, ILogger? logger)
	{
		ArgumentNullException.ThrowIfNull(options);
		options.Validate();
		// Snapshot mutable options so one client's key and host cannot change mid-request.
		var settings = new CiscoIqV1ClientOptions
		{
			ApiKey = options.ApiKey,
			Region = options.Region,
			BaseAddress = options.BaseAddress,
			SendRegionHeader = options.SendRegionHeader,
			HttpClientTimeoutSeconds = options.HttpClientTimeoutSeconds,
			RetryRateLimitedRequests = options.RetryRateLimitedRequests,
			MaxAttemptCount = options.MaxAttemptCount,
			UserAgent = options.UserAgent
		};
		var request = new CiscoIqV1RequestHandler(settings) { InnerHandler = transport };
		var retry = new CiscoIqV1RetryHandler(settings, info => Volatile.Write(ref _lastRateLimit, info), delay ?? Task.Delay) { InnerHandler = request };
		var errors = new CiscoIqV1ErrorHandler { InnerHandler = retry };
		_http = new HttpClient(errors)
		{
			BaseAddress = settings.EffectiveBaseAddress,
			Timeout = TimeSpan.FromSeconds(settings.HttpClientTimeoutSeconds)
		};
		var json = new JsonSerializerSettings
		{
			DateParseHandling = DateParseHandling.None,
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new CiscoIqV1ContractResolver()
		};
		json.Converters.Add(new CiscoIqV1DateOnlyConverter());
		json.Converters.Add(new CiscoIqV1OptionalConverter());
		var refit = new RefitSettings
		{
			ContentSerializer = new NewtonsoftJsonContentSerializer(json),
			UrlParameterFormatter = new CiscoIqV1UrlParameterFormatter(),
			TransportExceptionFactory = (_, exception, _) => exception
		};
		Assets = RestService.For<IAssetApi>(_http, refit);
		Assessments = RestService.For<IAssessmentApi>(_http, refit);
		Products = RestService.For<IProductApi>(_http, refit);
		Identity = RestService.For<IIamApi>(_http, refit);
		Platform = RestService.For<ISystemApi>(_http, refit);
		if (logger is not null)
		{
			LogInitialized(logger, settings.Region, settings.EffectiveBaseAddress.Host);
		}
	}

	/// <summary>The Asset API: physical and virtual assets, coverage, data sources and licensing.</summary>
	public IAssetApi Assets { get; }

	/// <summary>The Assessment API: assessments, findings, insights and recommendations.</summary>
	public IAssessmentApi Assessments { get; }

	/// <summary>The Product API: products, security advisories, field notices and end-of-life bulletins.</summary>
	public IProductApi Products { get; }

	/// <summary>The IAM API: accounts, users, groups, roles, policies, service accounts and API keys.</summary>
	public IIamApi Identity { get; }

	/// <summary>The System API: resource groups, preferences, query, bulk and schema discovery.</summary>
	public ISystemApi Platform { get; }

	/// <summary>The latest observed rate-limit headers, or null before any are received.</summary>
	public CiscoIqV1RateLimitInfo? LastRateLimit => Volatile.Read(ref _lastRateLimit);

	internal static HttpClientHandler CreateTransport() => new() { UseCookies = false, AllowAutoRedirect = false };

	[LoggerMessage(Level = LogLevel.Debug, Message = "Cisco IQ GA client initialized for region {Region} on host {Host}.")]
	private static partial void LogInitialized(ILogger logger, CiscoIqV1Region region, string host);

	/// <inheritdoc />
	public void Dispose() => _http.Dispose();
}
