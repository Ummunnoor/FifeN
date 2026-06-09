namespace Application.DTOs.Identity
{
    public class VerifyPhoneOtpDTO
    {
        public required string PhoneNumber { get; set; }
        public required string OtpCode { get; set; }
    }
}
