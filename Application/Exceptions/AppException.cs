using System;
using System.Collections.Generic;

namespace Application.Exceptions
{
    /// <summary>
    /// Base class for expected, mappable application errors. The <see cref="StatusCode"/> drives the
    /// HTTP response produced centrally by the exception-handling middleware (RFC 7807 ProblemDetails).
    /// </summary>
    public abstract class AppException : Exception
    {
        protected AppException(string message, int statusCode, string title)
            : base(message)
        {
            StatusCode = statusCode;
            Title = title;
        }

        /// <summary>HTTP status code to return.</summary>
        public int StatusCode { get; }

        /// <summary>Short, human-readable summary for the ProblemDetails title.</summary>
        public string Title { get; }

        /// <summary>Optional field-keyed validation errors.</summary>
        public IReadOnlyDictionary<string, string[]>? Errors { get; protected init; }
    }

    /// <summary>422 — a request was well-formed but violated a business rule.</summary>
    public sealed class BusinessRuleException(string message)
        : AppException(message, 422, "Business rule violation");

    /// <summary>404 — the requested resource does not exist (or is not visible to the caller).</summary>
    public sealed class NotFoundException(string message)
        : AppException(message, 404, "Resource not found");

    /// <summary>409 — the request conflicts with current state (duplicate, already actioned, concurrency).</summary>
    public sealed class ConflictException(string message)
        : AppException(message, 409, "Conflict");

    /// <summary>401 — the caller is not authenticated, or supplied invalid credentials/token.</summary>
    public sealed class UnauthorizedException(string message)
        : AppException(message, 401, "Unauthorized");

    /// <summary>403 — the caller is authenticated but not allowed to perform the action.</summary>
    public sealed class ForbiddenException(string message)
        : AppException(message, 403, "Forbidden");

    /// <summary>
    /// 401 — the first factor (OTP) was accepted but a second factor (authenticator code) is required
    /// and was not supplied. Distinguished from <see cref="UnauthorizedException"/> by its title so
    /// clients can prompt for the TOTP code and resubmit.
    /// </summary>
    public sealed class MfaRequiredException(string message)
        : AppException(message, 401, "MFA required");

    /// <summary>410 — the resource (e.g. an OTP challenge) has expired.</summary>
    public sealed class GoneException(string message)
        : AppException(message, 410, "Gone");

    /// <summary>423 — the resource is locked (e.g. an OTP challenge after too many failed attempts).</summary>
    public sealed class LockedException(string message)
        : AppException(message, 423, "Locked");

    /// <summary>429 — the caller exceeded a rate limit.</summary>
    public sealed class RateLimitException(string message)
        : AppException(message, 429, "Too many requests");

    /// <summary>413 — an uploaded payload exceeded the allowed size.</summary>
    public sealed class PayloadTooLargeException(string message)
        : AppException(message, 413, "Payload too large");

    /// <summary>415 — an uploaded file had an unsupported media type.</summary>
    public sealed class UnsupportedMediaTypeException(string message)
        : AppException(message, 415, "Unsupported media type");

    /// <summary>400 — request input failed validation. Carries field-level errors.</summary>
    public sealed class InputValidationException : AppException
    {
        public InputValidationException(IReadOnlyDictionary<string, string[]> errors)
            : base("One or more validation errors occurred.", 400, "Validation failed")
        {
            Errors = errors;
        }
    }
}
