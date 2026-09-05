using FluentValidation;

namespace FlowBox.Api.Endpoints.Shipment;

public class CreateShipmentValidator : AbstractValidator<CreateShipmentEndpoint.CreateShipmentRequest>
{
    public CreateShipmentValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty().WithMessage("Çıkış noktası (Origin) boş olamaz.");
            
        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Varış noktası (Destination) boş olamaz.");
            
        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Kargo ağırlığı sıfırdan büyük olmalıdır.");
    }
}