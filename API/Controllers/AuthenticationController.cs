using System.Text;
using Application.DTOs.Identity;
using Application.Services.Interfaces;
using Domain.Entities.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthenticationController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly IEmailSender<User> _emailSender;
        private readonly IConfiguration _configuration;
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;


        public AuthenticationController(IAuthenticationService authenticationService,
        IEmailSender<User> emailSender, SignInManager<User> signInManager, UserManager<User> userManager, IConfiguration configuration)
        {
            _authenticationService = authenticationService;
            _emailSender = emailSender;
            _configuration = configuration;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDTO createUserDTO)
        {
            var result = await _authenticationService.CreateUserAsync(createUserDTO);
            if (!result.Success)
                return BadRequest(result);
            if (result.Success) return Ok(result);
            return Created(string.Empty, result);

        }
        
        [HttpGet("confirmEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            var result = await _authenticationService.ConfirmEmailAsync(userId, code);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("resendConfirmationEmail")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmationEmail([FromBody] string email)
        {
            var result = await _authenticationService.ResendConfirmationEmailAsync(email);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("send-phone-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> SendPhoneOtp([FromBody] SendPhoneOtpDTO sendPhoneOtpDTO)
        {
            var result = await _authenticationService.SendPhoneOtpAsync(sendPhoneOtpDTO.PhoneNumber);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("verify-phone-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyPhoneOtp([FromBody] VerifyPhoneOtpDTO verifyPhoneOtpDTO)
        {
            var result = await _authenticationService.VerifyPhoneOtpAsync(verifyPhoneOtpDTO);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> LoginUser([FromBody] LoginUserDTO loginUserDTO)
        {
            var result = await _authenticationService.LoginUserAsync(loginUserDTO);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> ReviveToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _authenticationService.ReviveTokenAsync(request.RefreshToken);

            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }
    }
}
