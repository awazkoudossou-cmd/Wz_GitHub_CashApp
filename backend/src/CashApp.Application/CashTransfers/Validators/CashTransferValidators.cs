using CashApp.Application.CashTransfers.Dtos;
using FluentValidation;

namespace CashApp.Application.CashTransfers.Validators;

public class CreateCashTransferDtoValidator : AbstractValidator<CreateCashTransferDto>
{
    public CreateCashTransferDtoValidator()
    {
        RuleFor(x => x.SourceCashRegisterId).GreaterThan(0);
        RuleFor(x => x.DestinationCashRegisterId).GreaterThan(0);
        RuleFor(x => x.DestinationCashRegisterId)
            .NotEqual(x => x.SourceCashRegisterId)
            .WithMessage("Source et destination doivent être différentes.");
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3, 8);
        RuleFor(x => x.TransferDate).NotEmpty();
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(1000);
    }
}

public class CancelCashTransferDtoValidator : AbstractValidator<CancelCashTransferDto>
{
    public CancelCashTransferDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
