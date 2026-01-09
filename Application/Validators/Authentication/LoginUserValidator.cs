using Application.DTOs.Identity;
using FluentValidation;

namespace Application.Validators.Authentication
{
    public class LoginUserValidator : AbstractValidator<LoginUserDTO>
    {
        public LoginUserValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty()
                .WithMessage("Email is required.")
                .EmailAddress()
                .WithMessage("Email is invalid.");

            RuleFor(x => x.Password)
                .NotEmpty()
                .WithMessage("Password is required.");
        }
    }
}