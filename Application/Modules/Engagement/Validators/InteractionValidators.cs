using Application.Modules.Engagement.DTOs;
using FluentValidation;

namespace Application.Modules.Engagement.Validators
{
    /// <summary>Validates the buyer's express-interest payload (both fields optional).</summary>
    public class ExpressInterestValidator : AbstractValidator<ExpressInterestRequest>
    {
        public ExpressInterestValidator()
        {
            RuleFor(x => x.Message)
                .MaximumLength(500).WithMessage("Message cannot exceed 500 characters.");

            RuleFor(x => x.OfferPrice)
                .GreaterThan(0).When(x => x.OfferPrice.HasValue)
                .WithMessage("Offer price must be greater than ₦0.");
        }
    }

    /// <summary>Validates a vendor's lead status update.</summary>
    public class UpdateLeadValidator : AbstractValidator<UpdateLeadRequest>
    {
        public UpdateLeadValidator() =>
            RuleFor(x => x.Status).IsInEnum().WithMessage("A valid lead status is required.");
    }
}
