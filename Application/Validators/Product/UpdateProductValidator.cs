using Application.DTOs.Product;
using FluentValidation;

namespace Application.Validators.Product
{
    public class UpdateProductValidator : AbstractValidator<UpdateProductDTO>
    {
        public UpdateProductValidator()
        {
            RuleFor(x => x.Id).NotEmpty().WithMessage("Product ID is required.");

        }
    }
}