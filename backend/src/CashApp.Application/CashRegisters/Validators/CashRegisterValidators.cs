using CashApp.Application.CashRegisters.Dtos;
using FluentValidation;

namespace CashApp.Application.CashRegisters.Validators;

public class CreateCashRegisterDtoValidator : AbstractValidator<CreateCashRegisterDto>
{
    public CreateCashRegisterDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(40)
            .Matches("^[A-Z0-9_-]+$").WithMessage("Code : majuscules / chiffres / _ / - uniquement.");

        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3, 8);
        RuleFor(x => x.DefaultDirection).IsInEnum();
        RuleFor(x => x.DefaultPaymentMethod).IsInEnum();
    }
}

public class UpdateCashRegisterDtoValidator : AbstractValidator<UpdateCashRegisterDto>
{
    public UpdateCashRegisterDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3, 8);
        RuleFor(x => x.DefaultDirection).IsInEnum();
        RuleFor(x => x.DefaultPaymentMethod).IsInEnum();
    }
}
