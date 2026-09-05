using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Courier;
using FlowBox.Api.Endpoints.Shipment;
using FluentAssertions;
using Xunit;

namespace FlowBox.IntegrationTests.ShipmentTest;

public class ShipmentAssignmentAndHistoryEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public ShipmentAssignmentAndHistoryEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AssignCourier_And_GetHistory_Succeeds()
    {
        var courierReq = new CreateCourierEndpoint.CreateCourierRequest("Ahmet Kurye", "+905559876543");
        var courierRes = await _client.PostAsJsonAsync("/api/couriers", courierReq);
        var courier = await courierRes.Content.ReadFromJsonAsync<CreateCourierEndpoint.CreateCourierResponse>();

        var shipmentReq = new CreateShipmentEndpoint.CreateShipmentRequest("Ankara", "Izmir", 2.0m);
        var shipmentRes = await _client.PostAsJsonAsync("/api/shipments", shipmentReq);
        var createdShipment = await shipmentRes.Content.ReadFromJsonAsync<CreateShipmentEndpoint.CreateShipmentResponse>();

        var assignReq = new AssignShipmentEndpoint.AssignShipmentRequest(
            CourierId: courier!.Id
        );
        var assignResponse = await _client.PatchAsJsonAsync($"/api/shipments/{createdShipment!.TrackingNumber}/assign", assignReq);
        assignResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResponse = await _client.GetAsync($"/api/shipments/{createdShipment.TrackingNumber}/history");
        historyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var historyResult = await historyResponse.Content.ReadFromJsonAsync<GetShipmentHistoryEndpoint.GetShipmentHistoryResponse>();
        historyResult.Should().NotBeNull();
        historyResult!.TrackingNumber.Should().Be(createdShipment.TrackingNumber);
        historyResult.History.Should().NotBeEmpty();
        historyResult.History.First().CourierName.Should().Be("Ahmet Kurye");
    }
}