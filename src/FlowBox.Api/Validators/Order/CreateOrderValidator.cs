using FlowBox.Api.Endpoints.Order;
using FluentValidation;

namespace FlowBox.Api.Validators.Order;

public class CreateOrderValidator : AbstractValidator<CreateOrderEndpoint.CreateOrderRequest>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.Origin)
            .NotEmpty().WithMessage("Çıkış noktası (Origin) boş olamaz.");

        RuleFor(x => x.Destination)
            .NotEmpty().WithMessage("Varış noktası (Destination) boş olamaz.");

        RuleFor(x => x.Weight)
            .GreaterThan(0).WithMessage("Kargo ağırlığı sıfırdan büyük olmalıdır.");
    }
}
