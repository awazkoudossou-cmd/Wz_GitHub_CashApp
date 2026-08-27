using CashApp.Application.Users.Dtos;
using CashApp.Domain.Constants;
using FluentValidation;

namespace CashApp.Application.Users.Validators;

public class CreateUserDtoValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserDtoValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().MaximumLength(80)
            .Matches("^[a-zA-Z0-9._-]+$").WithMessage("Username : caractères alphanumériques uniquement.");

        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8).WithMessage("Le mot de passe doit comporter au moins 8 caractères.");

        RuleFor(x => x.RoleCode)
            .NotEmpty()
            .Must(r => RoleCodes.All.Contains(r))
            .WithMessage($"RoleCode doit être l'un de : {string.Join(", ", RoleCodes.All)}");
    }
}

public class UpdateUserDtoValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.RoleCode)
            .NotEmpty()
            .Must(r => RoleCodes.All.Contains(r))
            .WithMessage($"RoleCode doit être l'un de : {string.Join(", ", RoleCodes.All)}");
    }
}

public class ResetPasswordDtoValidator : AbstractValidator<ResetPasswordDto>
{
    public ResetPasswordDtoValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
