using FlowBox.Api.Data;
using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowBox.Api.Endpoints.Shipment;

public class AssignShipmentEndpoint : IEndpoint
{
    public record AssignShipmentRequest(Guid CourierId);

    public record AssignShipmentResponse(string TrackingNumber, Guid CourierId, string CourierName);

    private static async Task<Results<Ok<AssignShipmentResponse>, NotFound<string>, ValidationProblem>> Handle(
        string trackingNumber,
        [FromBody] AssignShipmentRequest request,
        FlowBoxDbContext db,
        IValidator<AssignShipmentRequest> validator,
        CancellationToken ctx)
    {
        var validationResult = await validator.ValidateAsync(request, ctx);
        if (!validationResult.IsValid)
        {
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        }

        var shipment = await db.Shipments
            .Include(s => s.Assignments)
            .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber, ctx);

        if (shipment is null) return TypedResults.NotFound("Kargo bulunamadı.");

        var courier = await db.Couriers.FirstOrDefaultAsync(c => c.Id == request.CourierId, ctx);
        if (courier is null) return TypedResults.NotFound("Belirtilen kurye bulunamadı.");

        var activeAssignment = shipment.Assignments.FirstOrDefault(a => a.IsActive);
        if (activeAssignment is not null)
        {
            activeAssignment.IsActive = false;
            activeAssignment.CompletedAt = DateTime.UtcNow;
        }

        var newAssignment = new FlowBox.Api.Models.ShipmentAssignment
        {
            ShipmentId = shipment.Id,
            CourierId = courier.Id
        };
        db.ShipmentAssignments.Add(newAssignment);

        await db.SaveChangesAsync(ctx);

        return TypedResults.Ok(new AssignShipmentResponse(shipment.TrackingNumber, courier.Id, courier.Name));
    }

    public void MapEndpoint(IEndpointRouteBuilder builder)
    {
        builder.MapPatch("/api/shipments/{trackingNumber}/assign", Handle)
            .WithOpenApi()
            .WithTags("Shipments")
            .WithSummary("Assigns a shipment to a specific courier.");
    }
}