using CashApp.Application.Variances.Dtos;
using FluentValidation;

namespace CashApp.Application.Variances.Validators;

public class CreateVarianceJustificationDtoValidator : AbstractValidator<CreateVarianceJustificationDto>
{
    public CreateVarianceJustificationDtoValidator()
    {
        RuleFor(x => x.Comment).NotEmpty().MaximumLength(2000);
    }
}
