using Application.DTOs.Category;
using FluentValidation;

namespace Application.Validators.Category
{
    public class UpdateCategoryValidator : BaseCategoryValidator<UpdateCategoryDTO>
    {
        public UpdateCategoryValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("Category ID is required.");
        }
    }
}