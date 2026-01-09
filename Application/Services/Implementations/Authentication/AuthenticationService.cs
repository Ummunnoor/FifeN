using Application.DTOs;
using Application.DTOs.Identity;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Logging;
using Application.Validators;
using AutoMapper;
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
            var validationResult =await _validationService.ValidateAsync(createUserDTO, _createUserValidator);
            if (!validationResult.Success)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: validationResult.Message
                );
            }

            // 2️⃣ Map DTO → User entity
            var user = _mapper.Map<User>(createUserDTO);
            user.UserName = createUserDTO.Email;

            // 3️⃣ Create the user (Identity hashes password internally)
            var created = await _userManagement.CreateUserAsync(user, createUserDTO.Password);
            if (!created)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: "Email already exists or user creation failed."
                );
            }

            // 4️⃣ Assign default role (always "User" / "Customer")
            var roleAssigned = await _roleManagement.AddUserToRoleAsync(user, "User");
            if (!roleAssigned)
            {
                // Rollback user if role assignment fails
                await _userManagement.RemoveUserByEmailAsync(user.Email!);

                return new BaseResponse<string>(
                    Success: false,
                    Message: "User created but role assignment failed."
                );
            }

            // 5️⃣ Log success
            _logger.LogInformation($"User {user.Id} created with default role 'User'");

            // 6️⃣ Return successful response
            return new BaseResponse<string>(
                Success: true,
                Message: "User created successfully",
                Data: user.Id
            );
        }


        public Task<LoginResponse> LoginUserAsync(LoginUserDTO loginUserDTO)
        {
            throw new NotImplementedException();
        }

        public Task<LoginResponse> ReviveTokenAsync(string refreshToken)
        {
            throw new NotImplementedException();
        }
    }
}
