using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public class ValidationService : IValidationService
    {
        public async Task<BaseResponse> ValidateAsync<T>(T model, IValidator<T> validator)
        {
            var validationResult = await validator.ValidateAsync(model);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                string errorMessage = string.Join("; ", errors);
                return new BaseResponse { Message = errorMessage };
            }
            return new BaseResponse { Success = true};
        }
    }
}