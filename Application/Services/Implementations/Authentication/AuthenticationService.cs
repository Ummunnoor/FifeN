using System.Security.Cryptography;
using System.Text;
using Application.DTOs;
using Application.DTOs.Identity;
using Application.Services.Interfaces;
using Application.Services.Interfaces.Authentication;
using Application.Services.Interfaces.Logging;
using Application.Validators;
using AutoMapper;
using Domain.Entities.Enums;
using Domain.Entities.Identity;
using Domain.Interfaces.Authentication;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Implementations.Authentication
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IUserManagement _userManagement;
        private readonly UserManager<User> _userManager;
        private readonly IEmailSender<User> _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ITokenManagement _tokenManagement;
        private readonly IRoleManagement _roleManagement;
        private readonly IGeneric<PhoneVerification> _phoneVerificationRepository;
        private readonly ISmsSender _smsSender;
        private readonly ITokenHasher _tokenHasher;
        private readonly IAppLogger<AuthenticationService> _logger;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUserDTO> _createUserValidator;
        private readonly IValidator<LoginUserDTO> _loginUserValidator;
        private readonly IValidationService _validationService;

        public AuthenticationService(
            IUserManagement userManagement,
            UserManager<User> userManager,
            IEmailSender<User> emailSender,
            IConfiguration configuration,
            ITokenManagement tokenManagement,
            IRoleManagement roleManagement,
            IGeneric<PhoneVerification> phoneVerificationRepository,
            ISmsSender smsSender,
            ITokenHasher tokenHasher,
            IAppLogger<AuthenticationService> logger,
            IMapper mapper,
            IValidator<CreateUserDTO> createUserValidator,
            IValidator<LoginUserDTO> loginUserValidator,
            IValidationService validationService)
        {
            _userManagement = userManagement;
            _userManager = userManager;
            _tokenManagement = tokenManagement;
            _roleManagement = roleManagement;
            _phoneVerificationRepository = phoneVerificationRepository;
            _smsSender = smsSender;
            _tokenHasher = tokenHasher;
            _logger = logger;
            _mapper = mapper;
            _createUserValidator = createUserValidator;
            _loginUserValidator = loginUserValidator;
            _validationService = validationService;
            _emailSender = emailSender;
            _configuration = configuration;
        }

        public async Task<BaseResponse<string>> ConfirmEmailAsync(string userId, string code)
        {
            var user = await _userManagement.GetUserByIdAsync(userId);
            if (user == null)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: "User not found."
                );
            }
            var decodedCode = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
            if (result.Succeeded)
            {
                return new BaseResponse<string>(
                    Success: true,
                    Message: "Email confirmed successfully."
                );
            }
            else
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: "Invalid or expired confirmation code."
                );
            }
           
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
                    await SendConfirmationEmailAsync(user);

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

        public async Task<BaseResponse<string>> ResendConfirmationEmailAsync(string email)
        {
           var user = await _userManagement.GetUserByEmailAsync(email);
            if (user == null)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: "User not found."
                );
            }
           if (user.EmailConfirmed)
            {
                return new BaseResponse<string>(
                    Success: false,
                    Message: "Email is already confirmed."
                );
            }
            await SendConfirmationEmailAsync(user);
            return new BaseResponse<string>(
                Success: true,
                Message: "Confirmation email resent successfully."
            );

        }

        public async Task<BaseResponse<string>> SendPhoneOtpAsync(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new BaseResponse<string>(false, "Phone number is required.");
            }

            var normalizedPhone = phoneNumber.Trim();
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);

            if (user == null)
            {
                var placeholderEmail = $"{normalizedPhone.TrimStart('+').Replace("@", string.Empty).Replace(" ", string.Empty)}@fifen.local";
                user = new User
                {
                    UserName = normalizedPhone,
                    PhoneNumber = normalizedPhone,
                    Email = placeholderEmail,
                    PhoneNumberConfirmed = false,
                    IsPhoneVerified = false
                };

                var createResult = await _userManager.CreateAsync(user);
                if (!createResult.Succeeded)
                {
                    return new BaseResponse<string>(false, "Could not create user for phone verification.");
                }

                await _roleManagement.AssignRoleAsync(user, AppRole.User);
            }

            var otpCode = GenerateOtpCode();
            var otpHash = _tokenHasher.Hash(otpCode);

            var verification = new PhoneVerification
            {
                UserId = user.Id,
                PhoneNumber = normalizedPhone,
                OtpCode = otpCode,
                OtpCodeHash = otpHash,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Purpose = "Authentication",
                IsVerified = false,
                AttemptCount = 0,
                MaxAttempts = 3,
                IsLocked = false
            };

            await _phoneVerificationRepository.AddAsync(verification);
            await _smsSender.SendSmsAsync(normalizedPhone, $"Your FifeN login code is {otpCode}. It expires in 10 minutes.");

            return new BaseResponse<string>(true, "OTP sent to phone.");
        }

        public async Task<LoginResponse> VerifyPhoneOtpAsync(VerifyPhoneOtpDTO verifyPhoneOtpDTO)
        {
            if (verifyPhoneOtpDTO == null || string.IsNullOrWhiteSpace(verifyPhoneOtpDTO.PhoneNumber) || string.IsNullOrWhiteSpace(verifyPhoneOtpDTO.OtpCode))
            {
                return new LoginResponse(false, "Phone number and OTP code are required.");
            }

            var normalizedPhone = verifyPhoneOtpDTO.PhoneNumber.Trim();
            var allVerifications = await _phoneVerificationRepository.GetAllAsync();
            var verification = allVerifications
                .Where(v => v.PhoneNumber == normalizedPhone && v.Purpose == "Authentication")
                .OrderByDescending(v => v.GeneratedAt)
                .FirstOrDefault();

            if (verification == null)
            {
                return new LoginResponse(false, "OTP request not found.");
            }

            if (verification.IsLocked)
            {
                return new LoginResponse(false, "OTP verification is locked. Please request a new code.");
            }

            if (verification.ExpiresAt < DateTime.UtcNow)
            {
                return new LoginResponse(false, "OTP code has expired. Please request a new one.");
            }

            var providedHash = _tokenHasher.Hash(verifyPhoneOtpDTO.OtpCode);
            if (providedHash != verification.OtpCodeHash)
            {
                verification.AttemptCount++;
                if (verification.AttemptCount >= verification.MaxAttempts)
                {
                    verification.IsLocked = true;
                    verification.LockedUntil = DateTime.UtcNow.AddMinutes(10);
                }

                await _phoneVerificationRepository.UpdateAsync(verification);
                return new LoginResponse(false, "Invalid OTP code.");
            }

            verification.IsVerified = true;
            verification.VerifiedAt = DateTime.UtcNow;
            await _phoneVerificationRepository.UpdateAsync(verification);

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == normalizedPhone);
            if (user == null)
            {
                return new LoginResponse(false, "User associated with the phone number could not be found.");
            }

            user.PhoneNumberConfirmed = true;
            user.IsPhoneVerified = true;
            user.PhoneVerifiedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            var roles = await _userManager.GetRolesAsync(user);
            if (!roles.Any())
            {
                await _roleManagement.AssignRoleAsync(user, AppRole.User);
            }

            var claims = await _userManagement.GetUserClaimsAsync(user);
            string jwtToken = _tokenManagement.GenerateToken(claims);
            string refreshToken = _tokenManagement.GetRefreshToken();
            var addRefreshTokenResult = await _tokenManagement.AddRefreshTokenAsync(user.Id, refreshToken);
            if (addRefreshTokenResult <= 0)
            {
                return new LoginResponse(false, "Failed to generate refresh token.");
            }

            return new LoginResponse(true, "OTP verified successfully.", jwtToken, refreshToken);
        }

        private string GenerateOtpCode(int length = 6)
        {
            var digits = new char[length];
            using var rng = RandomNumberGenerator.Create();
            var randomBytes = new byte[length];
            rng.GetBytes(randomBytes);

            for (var i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + (randomBytes[i] % 10));
            }

            return new string(digits);
        }

        private async Task SendConfirmationEmailAsync(User user)
        { 
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedCode = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var confirmationLink = $"{_configuration["ClientAppUrl"]}/confirm-email?userId={user.Id}&code={encodedCode}";
            await _emailSender.SendConfirmationLinkAsync(user, user.Email!, confirmationLink);
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
