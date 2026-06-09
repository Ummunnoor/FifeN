namespace Application.Services.Interfaces.Authentication
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }
}
