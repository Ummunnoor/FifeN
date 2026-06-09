using Application.Modules.TrustSafety.DTOs;
using FluentValidation;

namespace Application.Modules.TrustSafety.Validators
{
    /// <summary>Validates a buyer's report submission.</summary>
    public class CreateReportValidator : AbstractValidator<CreateReportRequest>
    {
        public CreateReportValidator()
        {
            RuleFor(x => x.TargetType).IsInEnum().WithMessage("A valid report target type is required.");
            RuleFor(x => x.Reason).IsInEnum().WithMessage("A valid report reason is required.");
            RuleFor(x => x.TargetId).NotEmpty().WithMessage("A report target is required.");
            RuleFor(x => x.Note).MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        }
    }

    /// <summary>Validates an admin's report resolution.</summary>
    public class ResolveReportValidator : AbstractValidator<ResolveReportRequest>
    {
        public ResolveReportValidator()
        {
            RuleFor(x => x.Status).IsInEnum().WithMessage("A valid report status is required.");
            RuleFor(x => x.Note).MaximumLength(500).WithMessage("Note cannot exceed 500 characters.");
        }
    }
}
