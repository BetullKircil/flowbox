using FlowBox.Api.Endpoints.Auth;
using FlowBox.Api.Enums;
using FluentValidation;

namespace FlowBox.Api.Validators.Auth;

public class CreateUserValidator : AbstractValidator<CreateUserEndpoint.CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta boş olamaz.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre boş olamaz.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Rol boş olamaz.")
            .IsEnumName(typeof(UserRole), caseSensitive: true)
            .WithMessage($"Geçersiz rol. Geçerli değerler: {string.Join(", ", Enum.GetNames<UserRole>())}");
    }
}
