using System.Threading;
using System.Threading.Tasks;
using Application.Modules.Identity.Services.Interfaces;
using Domain.Entities.Enums;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Persistence.Modules.Identity
{
    /// <summary>
    /// Development OTP sender that logs the code instead of dispatching it. The production Termii
    /// (WhatsApp-first → SMS fallback) sender will replace this behind the same <see cref="IOtpSender"/> port.
    /// The plaintext code is logged <strong>only in Development</strong>; outside it (where this stub may
    /// still be wired before the real sender lands) the code is never written to logs, since Serilog
    /// persists to disk and the code is a credential.
    /// </summary>
    public sealed class LoggingOtpSender(ILogger<LoggingOtpSender> logger, IHostEnvironment env) : IOtpSender
    {
        public Task<OtpChannel> SendAsync(string phoneNumber, string code, CancellationToken ct)
        {
            if (env.IsDevelopment())
                logger.LogInformation("[DEV OTP] {Phone} → {Code}", phoneNumber, code);
            else
                logger.LogWarning(
                    "OTP requested but no real sender is configured; the code was not dispatched. " +
                    "Wire a production IOtpSender (e.g. Termii) before going live.");

            return Task.FromResult(OtpChannel.WhatsApp);
        }
    }
}
