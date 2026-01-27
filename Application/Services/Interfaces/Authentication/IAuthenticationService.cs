using Application.DTOs;
using Application.DTOs.Identity;
namespace Application.Services.Interfaces
{
    public interface IAuthenticationService
    {
        Task<BaseResponse<string>> CreateUserAsync(CreateUserDTO createUserDTO);
        Task<LoginResponse> LoginUserAsync(LoginUserDTO loginUserDTO);
        Task<LoginResponse> ReviveTokenAsync(string refreshToken);
    }
}