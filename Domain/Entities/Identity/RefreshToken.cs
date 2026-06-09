using System;

namespace Domain.Entities.Identity
{
    /// <summary>
    /// A hashed, rotatable refresh token bound to a user. The raw token is returned to the client
    /// exactly once; only its hash is persisted.
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }

        /// <summary>Owning user.</summary>
        public Guid UserId { get; set; }

        /// <summary>SHA-256 hash of the raw token — never the plaintext.</summary>
        public string TokenHash { get; set; } = default!;

        /// <summary>When the token expires (UTC).</summary>
        public DateTimeOffset ExpiresAtUtc { get; set; }

        /// <summary>When the token was revoked, if it has been (rotation or logout).</summary>
        public DateTimeOffset? RevokedAtUtc { get; set; }

        /// <summary>IP that requested the token, for audit.</summary>
        public string? CreatedByIp { get; set; }

        /// <summary>When the token was issued (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>True when the token is neither expired nor revoked.</summary>
        public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTimeOffset.UtcNow;
    }
}
