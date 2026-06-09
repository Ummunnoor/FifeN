using Application.Modules.Catalog.DTOs;
using Domain.Entities.Enums;
using FluentValidation;

namespace Application.Modules.Catalog.Validators
{
    /// <summary>Shared rules for create/update listing payloads.</summary>
    public abstract class ProductWriteValidator<T> : AbstractValidator<T>
    {
        protected void ApplyCommonRules(
            System.Func<T, string> title,
            System.Func<T, string> description,
            System.Func<T, decimal> price,
            System.Func<T, PriceType> priceType,
            System.Func<T, ProductCondition> condition,
            System.Func<T, System.Guid> categoryId,
            System.Func<T, NigerianState> state,
            System.Func<T, string> city)
        {
            RuleFor(x => title(x))
                .NotEmpty().WithMessage("Title is required.")
                .MaximumLength(80).WithMessage("Title cannot exceed 80 characters.");

            RuleFor(x => description(x))
                .NotEmpty().WithMessage("Description is required.")
                .MaximumLength(1000).WithMessage("Description cannot exceed 1000 characters.");

            // Price must be positive unless the vendor chose "Contact for price".
            RuleFor(x => price(x))
                .GreaterThan(0).When(x => priceType(x) != PriceType.ContactForPrice)
                .WithMessage("Price must be greater than ₦0.");

            RuleFor(x => priceType(x)).IsInEnum().WithMessage("A valid price type is required.");
            RuleFor(x => condition(x)).IsInEnum().WithMessage("A valid condition is required.");
            RuleFor(x => state(x)).IsInEnum().WithMessage("A valid Nigerian state is required.");
            RuleFor(x => categoryId(x)).NotEmpty().WithMessage("A category is required.");
            RuleFor(x => city(x))
                .NotEmpty().WithMessage("City is required.")
                .MaximumLength(80).WithMessage("City cannot exceed 80 characters.");
        }
    }

    public class CreateProductValidator : ProductWriteValidator<CreateProductRequest>
    {
        public CreateProductValidator() => ApplyCommonRules(
            x => x.Title, x => x.Description, x => x.PriceAmount, x => x.PriceType,
            x => x.Condition, x => x.CategoryId, x => x.State, x => x.City);
    }

    public class UpdateProductValidator : ProductWriteValidator<UpdateProductRequest>
    {
        public UpdateProductValidator() => ApplyCommonRules(
            x => x.Title, x => x.Description, x => x.PriceAmount, x => x.PriceType,
            x => x.Condition, x => x.CategoryId, x => x.State, x => x.City);
    }
}
