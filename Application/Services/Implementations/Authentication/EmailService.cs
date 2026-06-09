using Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;
using Resend;

namespace Application.Services.Implementations.Authentication
{
    public class EmailService(IResend resend) : IEmailSender<User>
    {
        public async Task SendConfirmationLinkAsync(User user, string email, string confirmationLink)
        {
            var subject = "Confirm your email address";
            var body = $@"
                        <p>Dear {user.UserName},</p>
                        <p>Please confirm your email address by clicking the link below:</p>
                        <p><a href='{confirmationLink}'>Confirm Email</a></p>
                        <p>Thank you for joining our platform!</p>";
            await SendMailAsync(email, subject, body);
            
        }

        private async Task SendMailAsync(string email, string subject, string body)
        {
            var message = new EmailMessage
            {   
                From = "whatever@resend.dev",
                
                Subject = subject,
                HtmlBody = body
            };
            message.To.Add(email);
            // Console.WriteLine(message.HtmlBody);
            await resend.EmailSendAsync(message);
            await Task.CompletedTask;
        }

        public Task SendPasswordResetCodeAsync(User user, string email, string resetCode)
        {
            throw new NotImplementedException();
        }

        public Task SendPasswordResetLinkAsync(User user, string email, string resetLink)
        {
            throw new NotImplementedException();
        }
    }
}
