using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Cisco.Iq.Api.V1;

namespace Cisco.Iq.Api.Test.V1;

public partial class PipelineTests
{
	[Fact]
	public async Task Error_Unauthorised_IsAuthenticationExceptionThatNeverHoldsTheKey()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"type\":\"t\",\"title\":\"Unauthenticated\",\"status\":401,\"detail\":\"Invalid key\"}", HttpStatusCode.Unauthorized)
			.WithHeader("WWW-Authenticate", "Bearer realm=\"asset\""));

		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();

		var exception = (await act.Should().ThrowAsync<CiscoIqV1AuthenticationException>()).Which;
		exception.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
		exception.Message.Should().Be("Invalid key");
		exception.Challenge.Should().Contain("realm=\"asset\"");
		exception.ToString().Should().NotContain(V1Support.ApiKey);
	}

	[Fact]
	public async Task Error_UnauthorisedWithoutAProblemDocument_ExplainsTheLikelyCauses()
	{
		using var client = V1Support.Client(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized) { Content = new StringContent(string.Empty) });
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		(await act.Should().ThrowAsync<CiscoIqV1AuthenticationException>()).Which.Message.Should().Contain("region");
	}

	[Fact]
	public async Task Error_ForbiddenHtmlWithoutTraceparent_PointsAtTheQueryLimit()
	{
		using var client = V1Support.Client(_ => new HttpResponseMessage(HttpStatusCode.Forbidden)
		{
			Content = new StringContent("<html>blocked</html>", Encoding.UTF8, "text/html")
		});
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		(await act.Should().ThrowAsync<CiscoIqV1AuthorizationException>()).Which.Message.Should().Contain("2,048");
	}

	[Fact]
	public async Task Error_ForbiddenWithNoContentType_UsesAGenericMessage()
	{
		using var client = V1Support.Client(_ => new HttpResponseMessage(HttpStatusCode.Forbidden) { Content = new ByteArrayContent([]) });
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		(await act.Should().ThrowAsync<CiscoIqV1AuthorizationException>()).Which.Message.Should().Be("Cisco IQ returned HTTP 403.");
	}

	[Fact]
	public async Task Error_ForbiddenProblem_KeepsTheServerMessage()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"title\":\"Forbidden\",\"detail\":\"No access\",\"status\":403,\"errors\":[{\"code\":\"FORBIDDEN\"}]}", HttpStatusCode.Forbidden)
			.WithHeader("traceparent", "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01"));
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		var exception = (await act.Should().ThrowAsync<CiscoIqV1AuthorizationException>()).Which;
		exception.Message.Should().Be("No access");
		exception.HasErrorCode("FORBIDDEN").Should().BeTrue();
		exception.HasErrorCode("NOT_FORBIDDEN").Should().BeFalse();
		exception.Traceparent.Should().StartWith("00-0af7");
	}

	[Fact]
	public async Task Error_NotFound_IsNotFoundException()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"title\":\"Not Found\",\"status\":404}", HttpStatusCode.NotFound));
		Func<Task> act = () => client.Assets.GetAssetPhysicalAssetAsync("missing");
		(await act.Should().ThrowAsync<CiscoIqV1NotFoundException>()).Which.Message.Should().Be("Not Found");
	}

	[Fact]
	public async Task Error_MissingRegion_ExplainsTheFix()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"error\":\"Missing required region signal\"}", HttpStatusCode.BadRequest));
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		var exception = (await act.Should().ThrowAsync<CiscoIqV1ApiException>()).Which;
		exception.StatusCode.Should().Be(HttpStatusCode.BadRequest);
		exception.Message.Should().Contain("SendRegionHeader");
		exception.Problem.Should().BeNull();
	}

	[Fact]
	public async Task Error_OtherStatusWithoutAProblem_UsesAGenericMessage()
	{
		using var client = V1Support.Client(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("oops") });
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		var exception = (await act.Should().ThrowAsync<CiscoIqV1ApiException>()).Which;
		exception.Message.Should().Be("Cisco IQ returned HTTP 500.");
		exception.Problem.Should().BeNull();
		exception.HasErrorCode("INTERNAL_SERVER_ERROR").Should().BeFalse();
		exception.Traceparent.Should().BeNull();
	}

	[Fact]
	public async Task Error_ValidationProblem_ExposesErrorCodes()
	{
		using var client = V1Support.Client(_ => V1Support.Json("{\"title\":\"Validation Failed\",\"status\":422,\"errors\":[{\"code\":\"VALIDATION_FAILED\",\"field\":\"filter\"}]}", HttpStatusCode.UnprocessableEntity));
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		var exception = (await act.Should().ThrowAsync<CiscoIqV1ApiException>()).Which;
		exception.HasErrorCode("VALIDATION_FAILED").Should().BeTrue();
		exception.Problem!.Errors[0].Field.Should().Be("filter");
	}

	[Fact]
	public async Task Error_BodyThatIsNotJson_IsTreatedAsNoProblem()
	{
		using var client = V1Support.Client(_ => new HttpResponseMessage(HttpStatusCode.BadGateway) { Content = new StringContent("<html>") });
		Func<Task> act = () => client.Assets.ListAssetPhysicalAssetsAsync();
		(await act.Should().ThrowAsync<CiscoIqV1ApiException>()).Which.Problem.Should().BeNull();
	}
}
