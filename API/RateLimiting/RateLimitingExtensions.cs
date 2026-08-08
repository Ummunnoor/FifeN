using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;


namespace API.RateLimiting
{
    /// <summary>
    /// Per-IP request throttling (BRD §5.1 / §7): a general API ceiling of ~60 req/min/IP applied
    /// globally, plus a stricter <see cref="AuthPolicy"/> for the OTP/auth endpoints to blunt SMS-pumping
    /// and credential abuse. Rejections are emitted as RFC 7807 ProblemDetails to match the rest of the API.
    /// </summary>
    public static class RateLimitingExtensions
    {
        /// <summary>Named policy for auth endpoints; apply with <c>[EnableRateLimiting(AuthPolicy)]</c>.</summary>
        public const string AuthPolicy = "auth";

        private const int GeneralPermitPerMinute = 60;
        private const int AuthPermitPerMinute = 10;

        public static IServiceCollection AddApiRateLimiting(this IServiceCollection services) =>
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                // General ceiling: ~60 req/min per client IP across the whole API.
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = GeneralPermitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                // Stricter per-IP budget for auth endpoints (OTP request/verify, refresh, logout).
                options.AddPolicy(AuthPolicy, context =>
                    RateLimitPartition.GetFixedWindowLimiter(
                        ClientKey(context),
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = AuthPermitPerMinute,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, ct) =>
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter =
                            ((int)retryAfter.TotalSeconds).ToString(NumberFormatInfo.InvariantInfo);

                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status429TooManyRequests,
                        Title = "Too many requests",
                        Detail = "Rate limit exceeded. Please slow down and try again shortly.",
                        Type = "https://httpstatuses.io/429"
                    };
                    problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

                    context.HttpContext.Response.ContentType = "application/problem+json";
                    await context.HttpContext.Response.WriteAsJsonAsync(problem, problem.GetType(), ct);
                };
            });

        /// <summary>Partition key: the client IP, or a shared bucket when the address is unavailable.</summary>
        private static string ClientKey(HttpContext context) =>
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
