using CashApp.Application.Anomalies.Dtos;
using FluentValidation;

namespace CashApp.Application.Anomalies.Validators;

public class CreateAnomalyDtoValidator : AbstractValidator<CreateAnomalyDto>
{
    public CreateAnomalyDtoValidator()
    {
        RuleFor(x => x.Severity).IsInEnum();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.RelatedEntityType).MaximumLength(80);
    }
}

public class AssignAnomalyDtoValidator : AbstractValidator<AssignAnomalyDto>
{
    public AssignAnomalyDtoValidator() => RuleFor(x => x.AssignToUserId).GreaterThan(0);
}

public class ResolveAnomalyDtoValidator : AbstractValidator<ResolveAnomalyDto>
{
    public ResolveAnomalyDtoValidator() => RuleFor(x => x.ResolutionComment).NotEmpty().MaximumLength(2000);
}

public class AddAnomalyCommentDtoValidator : AbstractValidator<AddAnomalyCommentDto>
{
    public AddAnomalyCommentDtoValidator() => RuleFor(x => x.Body).NotEmpty().MaximumLength(2000);
}
