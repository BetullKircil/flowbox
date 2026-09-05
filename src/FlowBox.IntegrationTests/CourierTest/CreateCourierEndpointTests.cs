using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Courier;
using FluentAssertions;
using Xunit;

namespace FlowBox.IntegrationTests.CourierTest;

public class CreateCourierEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public CreateCourierEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateCourier_WithValidRequest_ReturnsCreated()
    {
        var request = new CreateCourierEndpoint.CreateCourierRequest("Test Kurye", "+905551234567");

        var response = await _client.PostAsJsonAsync("/api/couriers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateCourierEndpoint.CreateCourierResponse>();
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Kurye");
    }
}