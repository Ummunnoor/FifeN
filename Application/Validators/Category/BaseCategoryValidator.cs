using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs.Category;
using FluentValidation;

namespace Application.Validators.Category
{
    public class BaseCategoryValidator<T> : AbstractValidator<T> where T : BaseCategoryDTO
    {
        public BaseCategoryValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Category name is required.");

            RuleFor(x => x.ImageUrl)
                .Must(BeAValidHttpsUrl).WithMessage("Invalid HTTPS URL.");
        }
        private static bool BeAValidHttpsUrl(string? url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                   && uri.Scheme == Uri.UriSchemeHttps;
        }
    }
}