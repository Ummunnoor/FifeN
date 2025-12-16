using Application.DTOs.Product;
using FluentValidation;

namespace Application.Validators.Product
{
    public class ProductAttributeValidator : AbstractValidator<ProductAttributeDTO>
    {
        public ProductAttributeValidator()
        {
            RuleFor(x => x.Key)
                .NotEmpty().WithMessage("Attribute key is required.");
            RuleFor(x => x.Value)
            .NotEmpty().WithMessage("Attribute value like the color, size, material must be provided.");
        }
    }
}