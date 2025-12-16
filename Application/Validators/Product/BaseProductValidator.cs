using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Product;
using FluentValidation;

namespace Application.Validators.Product
{
    public abstract class BaseProductValidator<T> : AbstractValidator<T> where T : BaseProductDTO
    {
        protected BaseProductValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Product name is required.")
                .MaximumLength(100).WithMessage("Product name cannot exceed 100 characters.");

            RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Productdescription is required")
                .MaximumLength(500).WithMessage("Product description cannot exceed 500 characters.");

            RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0");

            RuleFor(x => x.Quantity)
            .GreaterThan(0).When(x => x.Quantity.HasValue)
            .WithMessage("Quantity must be greater than 0 if specified");

            RuleFor(x => x.ImageUrl)
            .NotEmpty().Must(BeAValidHttpsUrl).WithMessage("ImageUrl must be a valid HTTPS URL");

            RuleForEach(x => x.Attributes).SetValidator(new ProductAttributeValidator());

        }

        private static bool BeAValidHttpsUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}
