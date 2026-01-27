using Application.DTOs;
using Application.DTOs.Identity;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Logging;
using Application.Validators;
using AutoMapper;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using FluentValidation;

namespace Application.Services.Implementations.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserManagement _userManagement;
        private readonly ITokenManagement _tokenManagement;
        private readonly IRoleManagement _roleManagement;
        private readonly IAppLogger<AuthenticationService> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserDTO> _createUserValidator;
        private readonly IValidator<LoginUserDTO> _loginUserValidator;
        private readonly IValidationService _validationService;

        public AuthenticationService(
            IUserManagement userManagement,
            ITokenManagement tokenManagement,
            IRoleManagement roleManagement,
            IAppLogger<AuthenticationService> logger,
            IMapper mapper,
            IValidator<CreateUserDTO> createUserValidator,
            IValidator<LoginUserDTO> loginUserValidator,
            IValidationService validationService)
        {
            _userManagement = userManagement;
            _tokenManagement = tokenManagement;
            _roleManagement = roleManagement;
            _logger = logger;
            _mapper = mapper;
            _createUserValidator = createUserValidator;
            _loginUserValidator = loginUserValidator;
            _validationService = validationService;
        }

        public async Task<BaseResponse<string>> CreateUserAsync(CreateUserDTO createUserDTO)
        {
            // 1️⃣ Validate input
            var validationResult =
                await _validationService.ValidateAsync(createUserDTO, _createUserValidator);

            if (!validationResult.Success)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: validationResult.Message
                );
            }

            // 2️⃣ Map DTO → Identity User
            var user = _mapper.Map<User>(createUserDTO);
            user.UserName = createUserDTO.Email;
            user.Email = createUserDTO.Email;

            // 3️⃣ Create user (Identity hashes password internally)
            var created = await _userManagement.CreateUserAsync(user, createUserDTO.Password);
            if (!created)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: "Email already exists or user creation failed."
                );
            }
            try
            {
                // 4️⃣ Assign default role (EXPLICIT, single role)
                await _roleManagement.AssignRoleAsync(user, AppRole.User);

                _logger.LogInformation(
                    $"User '{user.Id}' created with role '{AppRole.User}'"
                );
            }
            catch
            {
                // 5️⃣ Rollback user if role assignment fails
                await _userManagement.RemoveUserByEmailAsync(user.Email!);
                throw; // Let middleware handle logging & response
            }

            // 6️⃣ Success response
            return new BaseResponse<string>(
                Success: true,
                Message: "User created successfully",
                Data: user.Id
            );
        }



        public async Task<LoginResponse> LoginUserAsync(LoginUserDTO loginUserDTO)
        {
            // 1️⃣ Validate input
            var validationResult =
                await _validationService.ValidateAsync(loginUserDTO, _loginUserValidator);

            if (!validationResult.Success)
            {
                return new LoginResponse(
                    Success: false,
                    Message: validationResult.Message
                );
            }
            var user = await _userManagement.LoginUserAsync(loginUserDTO.Email, loginUserDTO.Password);
            if (user == null)
            {
                _logger.LogWarning(
                    $"Failed login attempt for email '{loginUserDTO.Email}'"
                );
                return new LoginResponse(
                    Success: false,
                    Message: "Invalid email or password."
                );
            }
            var claims = await _userManagement.GetUserClaimsAsync(user);
            string jwtToken =  _tokenManagement.GenerateToken(claims);
            string refreshToken = _tokenManagement.GetRefreshToken();
            var addRefreshTokenResult = await _tokenManagement.AddRefreshTokenAsync(user!.Id, refreshToken);
           if (addRefreshTokenResult <= 0)
            {
                return new LoginResponse(
                    Success: false,
                    Message: "Failed to generate refresh token."
                );
            }
            return new LoginResponse(
                Success: true,
                Message: "Login successful.",
                Token: jwtToken,
                RefreshToken: refreshToken
            );
            
        }

        public async Task<LoginResponse> ReviveTokenAsync(string refreshToken)
        {
            var isValid = await _tokenManagement.ValidateRefreshTokenAsync(refreshToken);
            if (!isValid)
            {
                return new LoginResponse(
                    Success: false,
                    Message: "Invalid refresh token."
                );
            }
            var userId = await _tokenManagement.GetUserIdByRefreshTokenAsync(refreshToken);
            var user = await _userManagement.GetUserByIdAsync(userId);
            var claims = await _userManagement.GetUserClaimsAsync(user);
            string jwtToken =  _tokenManagement.GenerateToken(claims);
            string newRefreshToken = _tokenManagement.GetRefreshToken();
            var updateRefreshTokenResult = await _tokenManagement.UpdateRefreshTokenAsync(user.Id, newRefreshToken);
           if (updateRefreshTokenResult <= 0)
            {
                return new LoginResponse(
                    Success: false,
                    Message: "Failed to generate new refresh token."
                );
            }
            return new LoginResponse(
                Success: true,
                Message: "Token revived successfully.",
                Token: jwtToken,
                RefreshToken: newRefreshToken
            );
        }
    }
}
