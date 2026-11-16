using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Order;
using FlowBox.Api.Endpoints.Shipment;
using FlowBox.Api.Enums;
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
        var courier = await _client.CreateCourierAsync("Ahmet Kurye");

        await _client.AuthenticateAsAsync(UserRole.Customer);
        var orderReq = new CreateOrderEndpoint.CreateOrderRequest("Ankara", "Izmir", 2.0m);
        var orderRes = await _client.PostAsJsonAsync("/api/orders", orderReq);
        var createdShipment = await orderRes.Content.ReadFromJsonAsync<CreateOrderEndpoint.CreateOrderResponse>();

        await _client.LoginAsync(TestAuthHelper.SeedAdminEmail, TestAuthHelper.SeedAdminPassword);
        var assignReq = new AssignShipmentEndpoint.AssignShipmentRequest(
            CourierId: courier.CourierId
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
