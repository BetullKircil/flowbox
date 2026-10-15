using FlowBox.Api.Endpoints.Auth;
using FluentValidation;

namespace FlowBox.Api.Validators.Auth;

public class LoginValidator : AbstractValidator<LoginEndpoint.LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("E-posta boş olamaz.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Şifre boş olamaz.");
    }
}
