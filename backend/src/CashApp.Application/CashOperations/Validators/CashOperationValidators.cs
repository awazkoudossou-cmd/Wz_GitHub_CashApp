using CashApp.Application.CashOperations.Dtos;
using FluentValidation;

namespace CashApp.Application.CashOperations.Validators;

public class CreateCashOperationDtoValidator : AbstractValidator<CreateCashOperationDto>
{
    public CreateCashOperationDtoValidator()
    {
        RuleFor(x => x.CashSessionId).GreaterThan(0);
        RuleFor(x => x.OperationDate).NotEmpty();
        RuleFor(x => x.Direction).IsInEnum();
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Le montant doit être strictement positif.");
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ExternalReference).MaximumLength(100);
        RuleFor(x => x.ThirdPartyName).MaximumLength(200);
    }
}

public class UpdateCashOperationDtoValidator : AbstractValidator<UpdateCashOperationDto>
{
    public UpdateCashOperationDtoValidator()
    {
        RuleFor(x => x.OperationDate).NotEmpty();
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.PaymentMethod).IsInEnum();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ExternalReference).MaximumLength(100);
        RuleFor(x => x.ThirdPartyName).MaximumLength(200);
    }
}

public class CancelCashOperationDtoValidator : AbstractValidator<CancelCashOperationDto>
{
    public CancelCashOperationDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
