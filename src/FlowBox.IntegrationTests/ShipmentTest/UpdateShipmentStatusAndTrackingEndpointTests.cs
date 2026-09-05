using System.Net;
using System.Net.Http.Json;
using FlowBox.Api.Endpoints.Shipment;
using FluentAssertions;
using Xunit;

namespace FlowBox.IntegrationTests.ShipmentTest;

public class UpdateShipmentStatusAndTrackingEndpointTests : IClassFixture<IntegrationTestWebAppFactory>
{
    private readonly HttpClient _client;

    public UpdateShipmentStatusAndTrackingEndpointTests(IntegrationTestWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    private async Task<string> CreateShipmentAsync()
    {
        var request = new CreateShipmentEndpoint.CreateShipmentRequest("Istanbul", "Konya", 3.0m);
        var response = await _client.PostAsJsonAsync("/api/shipments", request);
        var created = await response.Content.ReadFromJsonAsync<CreateShipmentEndpoint.CreateShipmentResponse>();
        return created!.TrackingNumber;
    }

    [Fact]
    public async Task UpdateStatus_FollowingHappyPath_SucceedsAndBuildsFullTrackingTimeline()
    {
        var trackingNumber = await CreateShipmentAsync();

        string[] pipeline =
        [
            "PickedUp",
            "ArrivedAtSortingCenter",
            "Sorted",
            "InTransit",
            "ArrivedAtDistributionCenter",
            "OutForDelivery",
            "Delivered"
        ];

        foreach (var status in pipeline)
        {
            var response = await _client.PatchAsJsonAsync(
                $"/api/shipments/{trackingNumber}/status",
                new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest(status));

            response.StatusCode.Should().Be(HttpStatusCode.OK, $"transition to {status} should be allowed");
        }

        var trackingResponse = await _client.GetAsync($"/api/shipments/{trackingNumber}/tracking");
        trackingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var tracking = await trackingResponse.Content.ReadFromJsonAsync<GetShipmentTrackingEndpoint.GetShipmentTrackingResponse>();
        tracking.Should().NotBeNull();
        tracking!.Events.Should().HaveCount(8); // Created (ilk kayıt) + 7 statü geçişi
        tracking.Events.Select(e => e.Status).Should().ContainInOrder(
            "Created", "PickedUp", "ArrivedAtSortingCenter", "Sorted",
            "InTransit", "ArrivedAtDistributionCenter", "OutForDelivery", "Delivered");
    }

    [Fact]
    public async Task UpdateStatus_SkippingAheadInPipeline_ReturnsValidationProblem()
    {
        var trackingNumber = await CreateShipmentAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/status",
            new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest("Delivered"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateStatus_AfterTerminalStatusReached_IsRejected()
    {
        var trackingNumber = await CreateShipmentAsync();

        var failResponse = await _client.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/status",
            new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest("Failed"));
        failResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondAttempt = await _client.PatchAsJsonAsync(
            $"/api/shipments/{trackingNumber}/status",
            new UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest("PickedUp"));
        secondAttempt.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
