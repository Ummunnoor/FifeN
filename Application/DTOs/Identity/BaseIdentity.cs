namespace Application.DTOs.Identity
{
    public class BaseIdentity
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}