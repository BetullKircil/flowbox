using FlowBox.Api.Endpoints.Courier;
using FluentValidation;

namespace FlowBox.Api.Validators.Courier;

public class CreateCourierValidator : AbstractValidator<CreateCourierEndpoint.CreateCourierRequest>
{
    public CreateCourierValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Kurye adı boş olamaz.")
            .MinimumLength(3).WithMessage("Kurye adı en az 3 karakter olmalıdır.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon numarası boş olamaz.")
            .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Geçerli bir telefon numarası girin.");
    }
}
