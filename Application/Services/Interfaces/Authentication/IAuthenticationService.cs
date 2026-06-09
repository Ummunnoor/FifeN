using Application.DTOs;
using Application.DTOs.Identity;
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Application.Services.Interfaces
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<string>> CreateUserAsync(CreateUserDTO createUserDTO);
        Task<BaseResponse<string>> ConfirmEmailAsync(string userId, string code);
        Task<BaseResponse<string>> ResendConfirmationEmailAsync(string email);
        Task<LoginResponse> LoginUserAsync(LoginUserDTO loginUserDTO);
        Task<LoginResponse> ReviveTokenAsync(string refreshToken);
        Task<BaseResponse<string>> SendPhoneOtpAsync(string phoneNumber);
        Task<LoginResponse> VerifyPhoneOtpAsync(VerifyPhoneOtpDTO verifyPhoneOtpDTO);
    }
}