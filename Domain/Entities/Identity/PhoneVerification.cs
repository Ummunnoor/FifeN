using System;
using Domain.Entities.Enums;

namespace Domain.Entities.Identity
{
    /// <summary>
    /// A transient one-time passcode challenge for a phone number. The plaintext code is never
    /// stored — only its hash. Records are short-lived and pruned after expiry.
    /// </summary>
    public class PhoneVerification
    {
        public Guid Id { get; set; }

        /// <summary>The +234 phone number being verified.</summary>
        public string PhoneNumber { get; set; } = default!;

        /// <summary>Channel the code was sent over (WhatsApp-first, SMS fallback).</summary>
        public OtpChannel Channel { get; set; }

        /// <summary>SHA-256 hash of the OTP — never the plaintext.</summary>
        public string CodeHash { get; set; } = default!;

        /// <summary>When the code expires (UTC); five minutes after issue.</summary>
        public DateTimeOffset ExpiresAtUtc { get; set; }

        /// <summary>Number of incorrect verification attempts; three locks the challenge.</summary>
        public int Attempts { get; set; }

        /// <summary>Lifecycle state of the challenge.</summary>
        public OtpStatus Status { get; set; } = OtpStatus.Pending;

        /// <summary>When the challenge was created (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }
    }
}
