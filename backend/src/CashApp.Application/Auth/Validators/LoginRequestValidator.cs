using CashApp.Application.Auth.Dtos;
using FluentValidation;

namespace CashApp.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
