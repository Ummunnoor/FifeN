using Application.Modules.Vendors.DTOs;
using FluentValidation;

namespace Application.Modules.Vendors.Validators
{
    public class CreateVendorRequestValidator : AbstractValidator<CreateVendorRequestRequest>
    {
        public CreateVendorRequestValidator()
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty().WithMessage("Business name is required.")
                .MinimumLength(2).WithMessage("Business name is too short.")
                .MaximumLength(160).WithMessage("Business name cannot exceed 160 characters.");

            RuleFor(x => x.WhatsAppNumber)
                .NotEmpty().WithMessage("A WhatsApp number is required.");

            RuleFor(x => x.Method)
                .IsInEnum().WithMessage("A valid verification method (Nin or Cac) is required.");

            RuleFor(x => x.IdentifierToken)
                .NotEmpty().WithMessage("An identity number (NIN or CAC) is required.")
                .MinimumLength(7).WithMessage("The identity number appears invalid.");
        }
    }

    public class UpdateVendorProfileValidator : AbstractValidator<UpdateVendorProfileRequest>
    {
        public UpdateVendorProfileValidator()
        {
            RuleFor(x => x.WhatsAppNumber)
                .NotEmpty().WithMessage("A WhatsApp number is required.");
        }
    }
}
