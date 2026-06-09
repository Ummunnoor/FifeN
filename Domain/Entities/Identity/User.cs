using System;
using System.Collections.Generic;
using Domain.Entities.Enums;
using Domain.Entities.Vendors;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Identity
{
    /// <summary>
    /// A TradeNaija account. Every user is a buyer by default; the <see cref="IsVendor"/> and
    /// <see cref="IsAdmin"/> flags unlock additional capabilities. Authentication is phone + OTP;
    /// the inherited password/email fields are unused in the MVP.
    /// </summary>
    public class User : IdentityUser<Guid>
    {
        /// <summary>Given name, captured during onboarding.</summary>
        public string FirstName { get; set; } = default!;

        /// <summary>Family name, captured during onboarding.</summary>
        public string LastName { get; set; } = default!;

        /// <summary>Optional verified phone used only for account recovery — never a second login identity.</summary>
        public string? SecondaryPhoneNumber { get; set; }

        /// <summary>True once an approved <see cref="VendorProfile"/> exists.</summary>
        public bool IsVendor { get; set; }

        /// <summary>True for members of the admin team.</summary>
        public bool IsAdmin { get; set; }

        /// <summary>True for the founder; the only account allowed to add or remove admins.</summary>
        public bool IsOwner { get; set; }

        /// <summary>Account lifecycle state.</summary>
        public UserStatus Status { get; set; } = UserStatus.Active;

        /// <summary>When the account was created (UTC).</summary>
        public DateTimeOffset CreatedAtUtc { get; set; }

        /// <summary>Last time the user logged in, searched, or interacted (UTC). Drives the "active user" metric.</summary>
        public DateTimeOffset? LastActiveAtUtc { get; set; }

        /// <summary>Convenience display name combining first and last names.</summary>
        public string DisplayName => $"{FirstName} {LastName}".Trim();

        // Navigation properties
        /// <summary>The 1:1 vendor extension, present only once the user is an approved vendor.</summary>
        public VendorProfile? VendorProfile { get; set; }

        /// <summary>Refresh tokens issued to this user.</summary>
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
