using FlowBox.Api.Data.Ef.Models;

namespace FlowBox.Api.Service.Shipment;

public abstract record UpdateShipmentStatusResult
{
    public sealed record Success(Data.Ef.Models.Shipment Shipment, string OldStatus) : UpdateShipmentStatusResult;
    public sealed record ShipmentNotFound : UpdateShipmentStatusResult;
    public sealed record InvalidTransition(string Message) : UpdateShipmentStatusResult;
}

public abstract record AssignShipmentResult
{
    public sealed record Success(string TrackingNumber, Guid CourierId, string CourierName) : AssignShipmentResult;
    public sealed record ShipmentNotFound : AssignShipmentResult;
    public sealed record CourierNotFound : AssignShipmentResult;
}

public abstract record ShipmentHistoryResult
{
    public sealed record Found(string TrackingNumber, IReadOnlyList<ShipmentAssignment> History) : ShipmentHistoryResult;
    public sealed record NotFound : ShipmentHistoryResult;
}

public abstract record ShipmentTrackingResult
{
    public sealed record Found(string TrackingNumber, IReadOnlyList<ShipmentTrackingEvent> Events) : ShipmentTrackingResult;
    public sealed record NotFound : ShipmentTrackingResult;
}
