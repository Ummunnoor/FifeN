using Application.Modules.Engagement.DTOs;
using FluentValidation;

namespace Application.Modules.Engagement.Validators
{
    /// <summary>Shared rules for create/update review payloads.</summary>
    public abstract class ReviewWriteValidator<T> : AbstractValidator<T>
    {
        protected void ApplyCommonRules(System.Func<T, int> rating, System.Func<T, string?> text)
        {
            RuleFor(x => rating(x))
                .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
            RuleFor(x => text(x))
                .MaximumLength(500).WithMessage("Review cannot exceed 500 characters.");
        }
    }

    public class CreateReviewValidator : ReviewWriteValidator<CreateReviewRequest>
    {
        public CreateReviewValidator() => ApplyCommonRules(x => x.Rating, x => x.Text);
    }

    public class UpdateReviewValidator : ReviewWriteValidator<UpdateReviewRequest>
    {
        public UpdateReviewValidator() => ApplyCommonRules(x => x.Rating, x => x.Text);
    }

    /// <summary>Validates an admin review moderation request.</summary>
    public class ModerateReviewValidator : AbstractValidator<ModerateReviewRequest>
    {
        public ModerateReviewValidator()
        {
            RuleFor(x => x.Status).IsInEnum().WithMessage("A valid review status is required.");
            RuleFor(x => x.Reason).NotEmpty().WithMessage("A reason is required.");
        }
    }
}
