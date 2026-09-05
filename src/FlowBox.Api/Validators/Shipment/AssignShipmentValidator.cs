using FlowBox.Api.Endpoints.Shipment;
using FluentValidation;

namespace FlowBox.Api.Validators.Shipment;

public class AssignShipmentValidator : AbstractValidator<AssignShipmentEndpoint.AssignShipmentRequest>
{
    public AssignShipmentValidator()
    {
        RuleFor(x => x.CourierId)
            .NotEmpty().WithMessage("Kurye ID boş olamaz.");
    }
}
