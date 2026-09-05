using FlowBox.Api.Enums;
using FluentValidation;

namespace FlowBox.Api.Endpoints.Shipment;

public class UpdateShipmentStatusValidator : AbstractValidator<UpdateShipmentStatusEndpoint.UpdateShipmentStatusRequest>
{
    public UpdateShipmentStatusValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Statü boş olamaz.")
            .IsEnumName(typeof(ShipmentStatus), caseSensitive: true)
            .WithMessage(
                $"Geçersiz bir kargo statüsü. Lütfen geçerli bir değer girin: {string.Join(", ", Enum.GetNames<ShipmentStatus>())}");
    }
}