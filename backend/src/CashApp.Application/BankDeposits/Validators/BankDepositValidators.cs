using CashApp.Application.BankDeposits.Dtos;
using FluentValidation;

namespace CashApp.Application.BankDeposits.Validators;

public class CreateBankDepositDtoValidator : AbstractValidator<CreateBankDepositDto>
{
    public CreateBankDepositDtoValidator()
    {
        RuleFor(x => x.CashRegisterId).GreaterThan(0);
        RuleFor(x => x.DepositDate).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.CurrencyCode).NotEmpty().Length(3, 8);
        RuleFor(x => x.BankName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.AccountReference).MaximumLength(80);
        RuleFor(x => x.DepositSlipReference).MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CancelBankDepositDtoValidator : AbstractValidator<CancelBankDepositDto>
{
    public CancelBankDepositDtoValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
