using CashApp.Application.Accounting.Dtos;
using FluentValidation;

namespace CashApp.Application.Accounting.Validators;

public class CreateAccountingJournalDtoValidator : AbstractValidator<CreateAccountingJournalDto>
{
    public CreateAccountingJournalDtoValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class UpdateAccountingJournalDtoValidator : AbstractValidator<UpdateAccountingJournalDto>
{
    public UpdateAccountingJournalDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateAccountingAccountDtoValidator : AbstractValidator<CreateAccountingAccountDto>
{
    public CreateAccountingAccountDtoValidator()
    {
        RuleFor(x => x.AccountNumber).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Nature).IsInEnum();
    }
}

public class UpdateAccountingAccountDtoValidator : AbstractValidator<UpdateAccountingAccountDto>
{
    public UpdateAccountingAccountDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Nature).IsInEnum();
    }
}

public class UpdateAccountingSettingsDtoValidator : AbstractValidator<UpdateAccountingSettingsDto>
{
    public UpdateAccountingSettingsDtoValidator()
    {
        RuleFor(x => x.GenerationType).IsInEnum();
        RuleFor(x => x.GenerationMode).IsInEnum();
        RuleFor(x => x.NarrationTemplate).MaximumLength(500);

        RuleFor(x => x.CashAccountRootNumber).MaximumLength(20)
            .Matches("^[0-9]+$").When(x => !string.IsNullOrWhiteSpace(x.CashAccountRootNumber))
            .WithMessage("Racine des comptes de caisse : chiffres uniquement.");
        RuleFor(x => x.CashJournalRootCode).MaximumLength(20)
            .Matches("^[A-Za-z0-9_-]+$").When(x => !string.IsNullOrWhiteSpace(x.CashJournalRootCode))
            .WithMessage("Racine des codes journaux : lettres / chiffres / _ / - uniquement.");
        RuleFor(x => x.CashAccountNumberLength)
            .GreaterThan(0)
            .Must((dto, length) => !length.HasValue || string.IsNullOrWhiteSpace(dto.CashAccountRootNumber) || length!.Value >= dto.CashAccountRootNumber!.Trim().Length)
            .When(x => x.CashAccountNumberLength.HasValue)
            .WithMessage("La longueur doit être supérieure ou égale à la longueur de la racine.");
    }
}

public class AssignCashRegisterJournalDtoValidator : AbstractValidator<AssignCashRegisterJournalDto>
{
    public AssignCashRegisterJournalDtoValidator()
    {
        RuleFor(x => x.AccountingJournalId).GreaterThan(0);
    }
}

public class AssignCashRegisterAccountDtoValidator : AbstractValidator<AssignCashRegisterAccountDto>
{
    public AssignCashRegisterAccountDtoValidator()
    {
        RuleFor(x => x.AccountingAccountId).GreaterThan(0);
    }
}

public class AssignCategoryAccountDtoValidator : AbstractValidator<AssignCategoryAccountDto>
{
    public AssignCategoryAccountDtoValidator()
    {
        RuleFor(x => x.AccountingAccountId).GreaterThan(0);
    }
}

public class AccountingPreviewRequestDtoValidator : AbstractValidator<AccountingPreviewRequestDto>
{
    public AccountingPreviewRequestDtoValidator()
    {
        RuleFor(x => x.Template).NotNull().MaximumLength(500);
    }
}

public class UpdateAccountingEntryDtoValidator : AbstractValidator<UpdateAccountingEntryDto>
{
    public UpdateAccountingEntryDtoValidator()
    {
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Comment).MaximumLength(2000);
    }
}

public class GenerateAccountingEntriesDtoValidator : AbstractValidator<GenerateAccountingEntriesDto>
{
    public GenerateAccountingEntriesDtoValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x).Must(x => x.StartDate <= x.EndDate)
            .WithMessage("La date de début doit être antérieure ou égale à la date de fin.");
    }
}

public class EnqueueManualGenerationDtoValidator : AbstractValidator<EnqueueManualGenerationDto>
{
    public EnqueueManualGenerationDtoValidator()
    {
        RuleFor(x => x.StartDate).NotEmpty();
        RuleFor(x => x.EndDate).NotEmpty();
        RuleFor(x => x).Must(x => x.StartDate <= x.EndDate)
            .WithMessage("La date de début doit être antérieure ou égale à la date de fin.");
    }
}
