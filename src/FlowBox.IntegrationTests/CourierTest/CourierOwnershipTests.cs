using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Data.Ef;
using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Endpoints.Courier;
using FlowBox.Api.Endpoints.Order;
using FlowBox.Api.Endpoints.Shipment;
using FlowBox.Api.Enums;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FlowBox.IntegrationTests.CourierTest;

public class CourierOwnershipTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly IntegrationTestWebAppFactory _factory;

    public CourierOwnershipTests(IntegrationTestWebAppFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        await client.LoginAsync(TestAuthHelper.SeedAdminEmail, TestAuthHelper.SeedAdminPassword);
        return client;
    }

    private async Task<(HttpClient Client, TestCourier Courier)> CourierClientAsync()
    {
        var courier = await _factory.CreateClient().CreateCourierAsync();
        var client = _factory.CreateClient();
        await client.LoginAsync(courier.Email, courier.Password);
        return (client, courier);
    }

    private async Task<string> CreateShipmentAsync()
    {
        var customer = _factory.CreateClient();
        await customer.AuthenticateAsAsync(UserRole.Customer);
        var response = await customer.PostAsJsonAsync(
            "/api/orders", new CreateOrderEndpoint.CreateOrderRequest("Istanbul", "Konya", 3m));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CreateOrderEndpoint.CreateOrderResponse>();
        return created!.TrackingNumber;
    }

    private static async Task AssignAsync(HttpClient admin, string trackingNumber, Guid courierId)
    {
        var response = await admin.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/assign", new AssignShipmentEndpoint.AssignShipmentRequest(courierId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateUser_AsAdmin_WithCourierRole_CreatesCourierLinkedToTheUser()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/admin/users",
            new CreateUserEndpoint.CreateUserRequest(
                $"{Guid.NewGuid():N}@flowbox.test", "SecurePass123!", "Courier", "Ahmet Kurye", "+905559876543"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateUserEndpoint.CreateUserResponse>();
        body!.CourierId.Should().NotBeNull();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowBoxDbContext>();
        var courier = await db.Couriers.AsNoTracking().SingleAsync(c => c.Id == body.CourierId);
        courier.UserId.Should().Be(body.Id);
        courier.Name.Should().Be("Ahmet Kurye");
    }

    [Theory]
    [InlineData(null, "+905559876543")]
    [InlineData("Ahmet Kurye", null)]
    [InlineData("Ahmet Kurye", "not-a-phone")]
    public async Task CreateUser_WithCourierRoleButMissingOrInvalidProfile_ReturnsBadRequest(string? name, string? phone)
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync(
            "/api/admin/users",
            new CreateUserEndpoint.CreateUserRequest(
                $"{Guid.NewGuid():N}@flowbox.test", "SecurePass123!", "Courier", name, phone));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssignedCourier_CanReadAndUpdateTheShipment()
    {
        var trackingNumber = await CreateShipmentAsync();
        var admin = await AdminClientAsync();
        var (courierClient, courier) = await CourierClientAsync();
        await AssignAsync(admin, trackingNumber, courier.CourierId);

        (await courierClient.GetAsync($"/api/shipments/{trackingNumber}")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await courierClient.GetAsync($"/api/shipments/{trackingNumber}/tracking")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await courierClient.GetAsync($"/api/shipments/{trackingNumber}/history")).StatusCode.Should().Be(HttpStatusCode.OK);

        var update = await courierClient.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/status",
            new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest("PickedUp"));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UnassignedCourier_GetsNotFound_ForReadAndUpdate_AndStatusStaysUnchanged()
    {
        var trackingNumber = await CreateShipmentAsync();
        var admin = await AdminClientAsync();
        var (_, assigned) = await CourierClientAsync();
        var (strangerClient, _) = await CourierClientAsync();
        await AssignAsync(admin, trackingNumber, assigned.CourierId);

        (await strangerClient.GetAsync($"/api/shipments/{trackingNumber}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await strangerClient.GetAsync($"/api/shipments/{trackingNumber}/tracking")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await strangerClient.GetAsync($"/api/shipments/{trackingNumber}/history")).StatusCode.Should().Be(HttpStatusCode.NotFound);

        var update = await strangerClient.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/status",
            new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest("PickedUp"));
        update.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var current = await admin.GetFromJsonAsync<GetShipmentEndpoint.GetShipmentResponse>($"/api/shipments/{trackingNumber}");
        current!.Status.Should().Be("Created");
    }

    [Fact]
    public async Task CourierWithNoAssignmentAtAll_GetsNotFound()
    {
        var trackingNumber = await CreateShipmentAsync();
        var (courierClient, _) = await CourierClientAsync();

        var response = await courierClient.GetAsync($"/api/shipments/{trackingNumber}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AfterReassignment_PreviousCourierLosesAccess_AndNewCourierGainsIt()
    {
        var trackingNumber = await CreateShipmentAsync();
        var admin = await AdminClientAsync();
        var (firstClient, first) = await CourierClientAsync();
        var (secondClient, second) = await CourierClientAsync();

        await AssignAsync(admin, trackingNumber, first.CourierId);
        (await firstClient.GetAsync($"/api/shipments/{trackingNumber}")).StatusCode.Should().Be(HttpStatusCode.OK);

        await AssignAsync(admin, trackingNumber, second.CourierId);

        (await firstClient.GetAsync($"/api/shipments/{trackingNumber}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await secondClient.GetAsync($"/api/shipments/{trackingNumber}")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Admin_CanUpdateStatusOfAnyShipment_EvenWithoutAssignment()
    {
        var trackingNumber = await CreateShipmentAsync();
        var admin = await AdminClientAsync();

        var response = await admin.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/status",
            new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest("PickedUp"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task MyShipments_ReturnsOnlyShipmentsActivelyAssignedToTheCaller()
    {
        var first = await CreateShipmentAsync();
        var second = await CreateShipmentAsync();
        var third = await CreateShipmentAsync();
        var admin = await AdminClientAsync();
        var (clientA, courierA) = await CourierClientAsync();
        var (clientB, courierB) = await CourierClientAsync();

        await AssignAsync(admin, first, courierA.CourierId);
        await AssignAsync(admin, second, courierA.CourierId);
        await AssignAsync(admin, third, courierB.CourierId);
        await AssignAsync(admin, second, courierB.CourierId);

        var listA = await clientA.GetFromJsonAsync<List<GetMyShipmentsEndpoint.MyShipmentResponse>>("/api/couriers/me/shipments");
        var listB = await clientB.GetFromJsonAsync<List<GetMyShipmentsEndpoint.MyShipmentResponse>>("/api/couriers/me/shipments");

        listA!.Select(s => s.TrackingNumber).Should().BeEquivalentTo([first]);
        listB!.Select(s => s.TrackingNumber).Should().BeEquivalentTo([second, third]);
    }

    [Fact]
    public async Task MyShipments_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync("/api/couriers/me/shipments");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MyShipments_AsCustomer_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        await client.AuthenticateAsAsync(UserRole.Customer);

        var response = await client.GetAsync("/api/couriers/me/shipments");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
