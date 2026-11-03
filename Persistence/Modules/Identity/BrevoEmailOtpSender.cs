using System.Text;
using System.Text.Json;
using Application.Modules.Identity.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Persistence.Modules.Identity;

public sealed class BrevoEmailOtpSender(
    HttpClient client,
    IConfiguration configuration,
    ILogger<BrevoEmailOtpSender> logger) : IEmailOtpSender
{
    public async Task SendAsync(string email, string code, CancellationToken ct)
    {
        var key = configuration["Brevo:ApiKey"] ?? throw new InvalidOperationException("Brevo:ApiKey is not configured.");
        var senderEmail = configuration["Brevo:SenderEmail"] ?? throw new InvalidOperationException("Brevo:SenderEmail is not configured.");
        var payload = new { sender = new { email = senderEmail, name = configuration["Brevo:SenderName"] ?? "Trade9ja" },
            to = new[] { new { email } }, subject = "Your Trade9ja sign-in code",
            textContent = $"Your Trade9ja verification code is {code}. It expires in 10 minutes. If you did not request this, ignore this email." };
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("api-key", key);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await client.SendAsync(request, ct);
        if (response.IsSuccessStatusCode)
            return;

        var responseBody = await response.Content.ReadAsStringAsync(ct);
        string? errorCode = null;
        string? errorMessage = null;
        try
        {
            using var error = JsonDocument.Parse(responseBody);
            if (error.RootElement.TryGetProperty("code", out var providerCode))
                errorCode = providerCode.GetString();
            if (error.RootElement.TryGetProperty("message", out var message))
                errorMessage = message.GetString();
        }
        catch (JsonException)
        {
            logger.LogError("Brevo email request failed with HTTP {StatusCode} and a non-JSON error response.",
                (int)response.StatusCode);
            throw new InvalidOperationException(
                $"Brevo could not send the verification email (HTTP {(int)response.StatusCode}).");
        }

        logger.LogError(
            "Brevo email request failed with HTTP {StatusCode}. Provider code: {ProviderCode}; message: {ProviderMessage}",
            (int)response.StatusCode, errorCode, errorMessage);
        throw new InvalidOperationException(
            $"Brevo could not send the verification email (HTTP {(int)response.StatusCode}).");
    }
}
