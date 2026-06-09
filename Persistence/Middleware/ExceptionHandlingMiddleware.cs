using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Persistence.Middleware
{
    /// <summary>
    /// Translates unhandled exceptions into RFC 7807 <see cref="ProblemDetails"/> responses. Expected
    /// errors (<see cref="AppException"/>) map to their declared status; known Postgres violations map to
    /// 409/400; everything else is a 500 (with detail hidden outside Development).
    /// </summary>
    public class ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment env)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (AppException ex)
            {
                await WriteProblemAsync(context, ex.StatusCode, ex.Title, ex.Message, ex.Errors);
            }
            catch (UnauthorizedAccessException ex)
            {
                await WriteProblemAsync(context, StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException pg)
            {
                logger.LogWarning(ex, "Database constraint violation. SqlState: {SqlState}", pg.SqlState);
                var (status, title) = pg.SqlState switch
                {
                    PostgresErrorCodes.UniqueViolation => (StatusCodes.Status409Conflict, "Conflict"),
                    PostgresErrorCodes.ForeignKeyViolation => (StatusCodes.Status400BadRequest, "Invalid reference"),
                    PostgresErrorCodes.NotNullViolation => (StatusCodes.Status400BadRequest, "Missing required field"),
                    _ => (StatusCodes.Status500InternalServerError, "Database error")
                };
                await WriteProblemAsync(context, status, title, ProblemDetailFor(status));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception. Path: {Path} {Method}", context.Request.Path, context.Request.Method);
                var detail = env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
                await WriteProblemAsync(context, StatusCodes.Status500InternalServerError, "Server error", detail);
            }
        }

        private static string ProblemDetailFor(int status) => status switch
        {
            StatusCodes.Status409Conflict => "A record with the same key already exists.",
            StatusCodes.Status400BadRequest => "The request referenced invalid or missing data.",
            _ => "A database error occurred."
        };

        private static async Task WriteProblemAsync(
            HttpContext context, int status, string title, string detail,
            IReadOnlyDictionary<string, string[]>? errors = null)
        {
            var problem = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail,
                Type = $"https://httpstatuses.io/{status}"
            };
            problem.Extensions["traceId"] = context.TraceIdentifier;
            if (errors is not null)
                problem.Extensions["errors"] = errors;

            context.Response.StatusCode = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem, problem.GetType());
        }
    }
}
