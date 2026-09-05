using FlowBox.Api.Data;
using FlowBox.Api.Domain;
using FlowBox.Api.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Endpoints.Shipment;

public class UpdateShipmentStatusEndpoint : IEndpoint
{
    private static async Task<Results<Ok<UpdateShipmentStatusResponse>, NotFound, ValidationProblem>> Handle(
        string trackingNumber,
        [FromBody] UpdateShipmentStatusRequest request,
        FlowBoxDbContext db,
        IValidator<UpdateShipmentStatusRequest> validator,
        CancellationToken ctx)
    {
        var validationResult = await validator.ValidateAsync(request, ctx);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var shipment = await db.Shipments.FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ctx);

        if (shipment is null)
        {
            return TypedResults.NotFound();
        }

        var newStatus = Enum.Parse<ShipmentStatus>(request.Status);

        if (!ShipmentStatusTransitions.IsValidTransition(shipment.Status, newStatus))
        {
            var validNext = ShipmentStatusTransitions.GetValidNextStatuses(shipment.Status);
            var message = validNext.Count == 0
                ? $"Kargo şu anda '{shipment.Status}' durumunda olduğu için statüsü artık güncellenemez."
                : $"'{shipment.Status}' durumundan '{newStatus}' durumuna geçilemez. Geçerli sonraki durumlar: {string.Join(", ", validNext)}.";

            var error = new Dictionary<string, string[]> { { "Status", new[] { message } } };
            return TypedResults.ValidationProblem(error);
        }

        var oldStatus = shipment.Status.ToString();
        shipment.Status = newStatus;

        db.ShipmentTrackingEvents.Add(new FlowBox.Api.Models.ShipmentTrackingEvent
        {
            ShipmentId = shipment.Id,
            Status = newStatus
        });

        await db.SaveChangesAsync(ctx);

        var response = new UpdateShipmentStatusResponse(
            shipment.TrackingNumber,
            oldStatus,
            shipment.Status.ToString());

        return TypedResults.Ok(response);
    }

    public record UpdateShipmentStatusRequest(string Status);

    public record UpdateShipmentStatusResponse(string TrackingNumber, string OldStatus, string NewStatus);


    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPatch("/api/shipments/{trackingNumber}/status", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Updates the current status of a shipment.");
    }
}