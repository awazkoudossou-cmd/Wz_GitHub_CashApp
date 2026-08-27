using CashApp.Application.Categories.Dtos;
using CashApp.Domain.Enums;
using FluentValidation;

namespace CashApp.Application.Categories.Validators;

public class CreateCategoryDtoValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryDtoValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().MaximumLength(40)
            .Matches("^[A-Z0-9_]+$").WithMessage("Code : majuscules / chiffres / _ uniquement.");

        RuleFor(x => x.Label).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Direction).IsInEnum()
            .Must(d => d == OperationDirection.IN || d == OperationDirection.OUT);
    }
}

public class UpdateCategoryDtoValidator : AbstractValidator<UpdateCategoryDto>
{
    public UpdateCategoryDtoValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Direction).IsInEnum();
    }
}
