using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Data.Ef;
using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Endpoints.Order;
using FlowBox.Api.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowBox.IntegrationTests.OrderTest;

public class CustomerOwnershipTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private const string Password = "OwnershipPass123!";

    private readonly IntegrationTestWebAppFactory _factory;

    public CustomerOwnershipTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private static async Task<Guid> RegisterAndLoginCustomerAsync(HttpClient client)
    {
        var email = $"{Guid.NewGuid():N}@flowbox.test";
        var registerRes = await client.PostAsJsonAsync(
            "/api/auth/register", new RegisterEndpoint.RegisterRequest(email, Password));
        var registered = await registerRes.Content.ReadFromJsonAsync<RegisterEndpoint.RegisterResponse>();
        await client.LoginAsync(email, Password);
        return registered!.Id;
    }

    private static async Task<CreateOrderEndpoint.CreateOrderResponse> CreateOrderAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/api/orders", new CreateOrderEndpoint.CreateOrderRequest("Istanbul", "Konya", 3m));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CreateOrderEndpoint.CreateOrderResponse>())!;
    }

    [Fact]
    public async Task CreateOrder_StoresTheCallingCustomerAsOwner()
    {
        var client = _factory.CreateClient();
        var customerId = await RegisterAndLoginCustomerAsync(client);

        var created = await CreateOrderAsync(client);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoxDbContext>();
        var persisted = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == created.OrderId);
        persisted.CustomerId.Should().Be(customerId);
    }

    [Fact]
    public async Task Owner_CanReadOwnShipment_TrackingAndHistory()
    {
        var owner = _factory.CreateClient();
        await RegisterAndLoginCustomerAsync(owner);
        var created = await CreateOrderAsync(owner);

        (await owner.GetAsync($"/api/shipments/{created.TrackingNumber}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await owner.GetAsync($"/api/shipments/{created.TrackingNumber}/tracking")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await owner.GetAsync($"/api/shipments/{created.TrackingNumber}/history")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnotherCustomer_GetsNotFound_ForShipmentTrackingAndHistory()
    {
        var owner = _factory.CreateClient();
        await RegisterAndLoginCustomerAsync(owner);
        var created = await CreateOrderAsync(owner);

        var stranger = _factory.CreateClient();
        await RegisterAndLoginCustomerAsync(stranger);

        (await stranger.GetAsync($"/api/shipments/{created.TrackingNumber}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await stranger.GetAsync($"/api/shipments/{created.TrackingNumber}/tracking")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await stranger.GetAsync($"/api/shipments/{created.TrackingNumber}/history")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Admin_CanReadAnyCustomersShipment()
    {
        var owner = _factory.CreateClient();
        await RegisterAndLoginCustomerAsync(owner);
        var created = await CreateOrderAsync(owner);

        var admin = _factory.CreateClient();
        await admin.AuthenticateAsAsync(UserRole.Admin);

        (await admin.GetAsync($"/api/shipments/{created.TrackingNumber}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await admin.GetAsync($"/api/shipments/{created.TrackingNumber}/tracking")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MyOrders_ReturnsOnlyTheCallersOrders()
    {
        var me = _factory.CreateClient();
        await RegisterAndLoginCustomerAsync(me);
        var mine1 = await CreateOrderAsync(me);
        var mine2 = await CreateOrderAsync(me);

        var other = _factory.CreateClient();
        await RegisterAndLoginCustomerAsync(other);
        var theirs = await CreateOrderAsync(other);

        var response = await me.GetAsync("/api/orders/mine");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var orders = await response.Content.ReadFromJsonAsync<List<GetMyOrdersEndpoint.MyOrderResponse>>();

        orders!.Select(o => o.TrackingNumber).Should()
            .BeEquivalentTo(new[] { mine1.TrackingNumber, mine2.TrackingNumber });
        orders.Should().NotContain(o => o.TrackingNumber == theirs.TrackingNumber);
        orders.Should().OnlyContain(o => o.Status == "Created");
    }

    [Fact]
    public async Task MyOrders_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/orders/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MyOrders_AsCourier_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await client.AuthenticateAsAsync(UserRole.Courier);

        var response = await client.GetAsync("/api/orders/mine");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
