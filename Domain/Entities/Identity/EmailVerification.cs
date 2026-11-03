namespace Domain.Entities.Identity;

public class EmailVerification
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string CodeHash { get; set; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public int Attempts { get; set; }
    public bool IsConsumed { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}
