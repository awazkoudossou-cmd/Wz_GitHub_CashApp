using CashApp.Application.Approvals.Dtos;
using CashApp.Domain.Constants;
using FluentValidation;

namespace CashApp.Application.Approvals.Validators;

public class CreateApprovalRuleDtoValidator : AbstractValidator<CreateApprovalRuleDto>
{
    public CreateApprovalRuleDtoValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(60).Matches("^[A-Z0-9_]+$");
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.TargetType).IsInEnum();
        RuleFor(x => x.AmountThreshold).GreaterThanOrEqualTo(0).When(x => x.AmountThreshold.HasValue);
        RuleFor(x => x.CurrencyCode).Length(3, 8).When(x => !string.IsNullOrWhiteSpace(x.CurrencyCode));
        RuleFor(x => x.RequiredApproverRole).NotEmpty()
            .Must(r => r == RoleCodes.Admin || r == RoleCodes.Supervisor)
            .WithMessage($"Doit être '{RoleCodes.Admin}' ou '{RoleCodes.Supervisor}'.");
    }
}

public class UpdateApprovalRuleDtoValidator : AbstractValidator<UpdateApprovalRuleDto>
{
    public UpdateApprovalRuleDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RequiredApproverRole).NotEmpty();
    }
}

public class ApproveRequestDtoValidator : AbstractValidator<ApproveRequestDto>
{
    public ApproveRequestDtoValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(1000);
    }
}

public class RejectRequestDtoValidator : AbstractValidator<RejectRequestDto>
{
    public RejectRequestDtoValidator()
    {
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(1000)
            .WithMessage("Un motif de rejet est obligatoire.");
    }
}
