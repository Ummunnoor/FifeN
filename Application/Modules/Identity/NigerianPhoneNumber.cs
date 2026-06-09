using System.Linq;

namespace Application.Modules.Identity
{
    /// <summary>
    /// Normalizes and validates Nigerian (+234) mobile numbers. Accepts local (<c>0803...</c>),
    /// national (<c>803...</c>), and international (<c>+234803...</c> / <c>234803...</c>) forms and
    /// canonicalizes to E.164 (<c>+234XXXXXXXXXX</c>).
    /// </summary>
    public static class NigerianPhoneNumber
    {
        private const string CountryCode = "234";

        /// <summary>
        /// Attempts to canonicalize <paramref name="input"/> to E.164. Returns false for anything that is
        /// not a plausible Nigerian mobile number (10 national digits beginning 7, 8, or 9).
        /// </summary>
        public static bool TryNormalize(string? input, out string normalized)
        {
            normalized = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var digits = new string(input.Where(char.IsDigit).ToArray());

            // Reduce to the 10-digit national number.
            string national;
            if (digits.StartsWith(CountryCode) && digits.Length == 13)
                national = digits[3..];
            else if (digits.Length == 11 && digits[0] == '0')
                national = digits[1..];
            else if (digits.Length == 10)
                national = digits;
            else
                return false;

            // Nigerian mobile national numbers are 10 digits starting 7/8/9.
            if (national.Length != 10 || national[0] is not ('7' or '8' or '9'))
                return false;

            normalized = $"+{CountryCode}{national}";
            return true;
        }
    }
}
