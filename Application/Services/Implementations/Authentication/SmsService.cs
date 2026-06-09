using Application.Services.Interfaces.Authentication;
using Application.Services.Interfaces.Logging;

namespace Application.Services.Implementations.Authentication
{
    public class SmsService : ISmsSender
    {
        private readonly IAppLogger<SmsService> _logger;

        public SmsService(IAppLogger<SmsService> logger)
        {
            _logger = logger;
        }

        public Task SendSmsAsync(string phoneNumber, string message)
        {
            _logger.LogInformation($"Sending OTP SMS to {phoneNumber}: {message}");
            return Task.CompletedTask;
        }
    }
}
