namespace Application.DTOs.Identity
{
    public class CreateUserDTO : BaseIdentity
    {
        public required string FullName { get; set; }
        public required string ConfirmPassword { get; set; }
    }
}