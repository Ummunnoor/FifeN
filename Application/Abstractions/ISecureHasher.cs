namespace Application.Abstractions
{
    /// <summary>
    /// One-way hashing for secrets that must be compared but never recovered — OTP codes and refresh
    /// tokens. Implemented with SHA-256 in the persistence layer.
    /// </summary>
    public interface ISecureHasher
    {
        /// <summary>Returns a stable hash of <paramref name="value"/>.</summary>
        string Hash(string value);
    }
}
