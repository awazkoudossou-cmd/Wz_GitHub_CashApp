using CashApp.Application.CashSessions.Dtos;
using FluentValidation;

namespace CashApp.Application.CashSessions.Validators;

public class OpenCashSessionDtoValidator : AbstractValidator<OpenCashSessionDto>
{
    public OpenCashSessionDtoValidator()
    {
        RuleFor(x => x.CashRegisterId).GreaterThan(0);
        RuleFor(x => x.OpeningBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Le solde d'ouverture doit être supérieur ou égal à 0.");
        RuleFor(x => x.OpenComment).MaximumLength(1000);
    }
}

public class CloseCashSessionDtoValidator : AbstractValidator<CloseCashSessionDto>
{
    public CloseCashSessionDtoValidator()
    {
        RuleFor(x => x.PhysicalBalance)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Le solde physique doit être supérieur ou égal à 0.");
        RuleFor(x => x.CloseComment).MaximumLength(1000);
    }
}
