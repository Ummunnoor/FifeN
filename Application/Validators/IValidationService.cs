using Application.DTOs;
using FluentValidation;

namespace Application.Validators
{
    public interface IValidationService
    {
        Task<BaseResponse> ValidateAsync<T>(T model, IValidator<T> validator);
    }
}