using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Data.Ef;
using FlowBox.Api.Endpoints.Order;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowBox.IntegrationTests.OrderTest;

public class CreateOrderEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;
    private readonly HttpClient _client;

    public CreateOrderEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateOrder_WithValidRequest_ReturnsCreatedWithShipmentTrackingNumber()
    {
        var request = new CreateOrderEndpoint.CreateOrderRequest(
            Origin: "Istanbul",
            Destination: "Konya",
            Weight: 3.5m
        );

        var response = await _client.PostAsJsonAsync("/api/orders", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateOrderEndpoint.CreateOrderResponse>();
        result.Should().NotBeNull();
        result!.OrderId.Should().NotBeEmpty();
        result.ShipmentId.Should().NotBeEmpty();
        result.TrackingNumber.Should().StartWith("TR");
        result.Status.Should().Be("Created");

        await AssertPersistedOrderAndShipmentAreLinked(result);
    }

    /// <summary>
    /// Response DTO'suna güvenmek yerine gerçek veritabanına bakarak Order <-> Shipment
    /// bağlantısının (OrderId FK) gerçekten persist edildiğini doğrular. Bu, unit testlerin
    /// (sahte repository ile) hiç yakalayamayacağı bir şey — gerçek EF/Postgres davranışı.
    /// </summary>
    private async Task AssertPersistedOrderAndShipmentAreLinked(CreateOrderEndpoint.CreateOrderResponse result)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoxDbContext>();

        var persistedOrder = await db.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == result.OrderId);
        persistedOrder.Should().NotBeNull("order HTTP response'ta dönüldüyse DB'de de kalıcı olmalı");

        var persistedShipment = await db.Shipments
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == result.ShipmentId);
        persistedShipment.Should().NotBeNull("shipment HTTP response'ta dönüldüyse DB'de de kalıcı olmalı");
        persistedShipment!.OrderId.Should().Be(result.OrderId, "Shipment'in FK'sı, oluşturduğu Order'ı işaret etmeli");
        persistedShipment.TrackingNumber.Should().Be(result.TrackingNumber);
    }
}
