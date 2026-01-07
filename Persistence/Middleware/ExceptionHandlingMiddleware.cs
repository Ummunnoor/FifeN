using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Application.DTOs;

namespace Persistence.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx)
            {
                await HandlePostgresException(context, ex, pgEx);
            }
            catch (Exception ex)
            {
                await HandleGenericException(context, ex);
            }
        }

        private async Task HandlePostgresException(
            HttpContext context,
            DbUpdateException ex,
            PostgresException pgEx)
        {
            context.Response.ContentType = "application/json";

            HttpStatusCode statusCode;
            string message;

            _logger.LogError(pgEx,
                "PostgreSQL error occurred. SqlState: {SqlState}",
                pgEx.SqlState);

            switch (pgEx.SqlState)
            {
                case PostgresErrorCodes.UniqueViolation:
                    statusCode = HttpStatusCode.Conflict;
                    message = "A record with the same key already exists.";
                    _logger.LogWarning(ex, "Unique constraint violation");
                    break;

                case PostgresErrorCodes.ForeignKeyViolation:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "Invalid reference to a related entity.";
                    _logger.LogWarning(ex, "Foreign key constraint violation");
                    break;

                case PostgresErrorCodes.NotNullViolation:
                    statusCode = HttpStatusCode.BadRequest;
                    message = "A required field is missing.";
                    _logger.LogWarning(ex, "Not-null constraint violation");
                    break;

                default:
                    statusCode = HttpStatusCode.InternalServerError;
                    message = "A database error occurred.";
                    _logger.LogError(ex, "Unhandled PostgreSQL error");
                    break;
            }

            await WriteResponse(context, statusCode, message);
        }

        private async Task HandleGenericException(HttpContext context, Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception. Path: {Path}, Method: {Method}, TraceId: {TraceId}",
                context.Request.Path,
                context.Request.Method,
                context.TraceIdentifier);

            await WriteResponse(
                context,
                HttpStatusCode.InternalServerError,
                "An unexpected error occurred.");
        }

        private static async Task WriteResponse(
            HttpContext context,
            HttpStatusCode statusCode,
            string message)
        {
            context.Response.StatusCode = (int)statusCode;

            var response = new BaseResponse<object>(
                Success: false,
                Message: message,
                Data: null
            );

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response));
        }
    }
}
