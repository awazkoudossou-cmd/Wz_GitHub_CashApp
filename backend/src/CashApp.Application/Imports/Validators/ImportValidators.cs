using CashApp.Application.Imports.Dtos;
using FluentValidation;

namespace CashApp.Application.Imports.Validators;

public class ConfirmImportDtoValidator : AbstractValidator<ConfirmImportDto>
{
    public ConfirmImportDtoValidator()
    {
        // booléen simple, pas de règle métier ici.
    }
}
