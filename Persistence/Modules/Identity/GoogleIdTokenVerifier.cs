using System.Net.Http.Json;
using Application.Modules.Identity.Services.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Persistence.Modules.Identity;

public sealed class GoogleIdTokenVerifier(HttpClient client, IConfiguration configuration) : IGoogleIdTokenVerifier
{
    public async Task<GoogleUserIdentity?> VerifyAsync(string token, CancellationToken ct)
    {
        var clientId = configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId)) return null;
        using var result = await client.GetAsync("https://oauth2.googleapis.com/tokeninfo?id_token=" + Uri.EscapeDataString(token), ct);
        if (!result.IsSuccessStatusCode) return null;
        var payload = await result.Content.ReadFromJsonAsync<TokenInfo>(cancellationToken: ct);
        if (payload is null || payload.aud != clientId || payload.email_verified != "true" ||
            string.IsNullOrWhiteSpace(payload.sub) || string.IsNullOrWhiteSpace(payload.email)) return null;
        return new GoogleUserIdentity(payload.sub, payload.email, payload.given_name, payload.family_name);
    }
    private sealed record TokenInfo(string? aud, string? sub, string? email, string? email_verified, string? given_name, string? family_name);
}
