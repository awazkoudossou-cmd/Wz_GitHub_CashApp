using CashApp.Application.Reconciliation.Dtos;
using FluentValidation;

namespace CashApp.Application.Reconciliation.Validators;

public class CreateReconciliationBatchDtoValidator : AbstractValidator<CreateReconciliationBatchDto>
{
    public CreateReconciliationBatchDtoValidator()
    {
        RuleFor(x => x.BatchType).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public class ReconcileItemsDtoValidator : AbstractValidator<ReconcileItemsDto>
{
    public ReconcileItemsDtoValidator()
    {
        RuleFor(x => x.Pairs).NotNull().NotEmpty();
        RuleForEach(x => x.Pairs).ChildRules(p =>
        {
            p.RuleFor(pp => pp.LeftEntityType).NotEmpty().MaximumLength(80);
            p.RuleFor(pp => pp.LeftEntityId).GreaterThan(0);
        });
    }
}
