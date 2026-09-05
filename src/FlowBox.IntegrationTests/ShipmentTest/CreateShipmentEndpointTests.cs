using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Shipment;
using FluentAssertions;
using Xunit;

namespace FlowBox.IntegrationTests.ShipmentTest;

public class CreateShipmentEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public CreateShipmentEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateShipment_WithValidRequest_ReturnsCreated()
    {
        var request = new CreateShipmentEndpoint.CreateShipmentRequest(
            Origin: "Istanbul",
            Destination: "Konya",
            Weight: 3.5m
        );

        var response = await _client.PostAsJsonAsync("/api/shipments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateShipmentEndpoint.CreateShipmentResponse>();
        result.Should().NotBeNull();
        result!.TrackingNumber.Should().StartWith("TR");
    }
}