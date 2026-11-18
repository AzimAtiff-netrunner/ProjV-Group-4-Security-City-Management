using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CmSecurityService.Common;

/// <summary>
/// Shared error response for every Security endpoint, as defined in the
/// Security Service API Details ("Standard Error Format").
/// </summary>
/// <remarks>
/// Example: <c>{ "error": "call_not_found", "message": "No 9-1-1 call exists with id ...", "traceId": "..." }</c>
/// </remarks>
public class ApiError
{
    /// <summary>Short machine-readable code, e.g. "invalid_query" or "call_not_found".</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>Human-readable description of what went wrong.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Request trace id, so a failing request can be found in the logs.</summary>
    public string TraceId { get; set; } = string.Empty;

    /// <summary>
    /// Creates an error for the current request.
    /// </summary>
    /// <param name="httpContext">The current request, if any (null in unit tests).</param>
    /// <param name="error">Machine-readable error code.</param>
    /// <param name="message">Human-readable description.</param>
    /// <returns>The error, with the request's trace id filled in.</returns>
    public static ApiError Create(HttpContext? httpContext, string error, string message) => new()
    {
        Error = error,
        Message = message,
        TraceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier ?? string.Empty
    };
}

/// <summary>
/// Makes ASP.NET's automatic validation errors use <see cref="ApiError"/> too.
/// </summary>
public static class ApiErrorServiceCollectionExtensions
{
    /// <summary>
    /// Replaces the default 400 response for invalid input (e.g. <c>?page=abc</c> or a malformed
    /// JSON body) with the shared <see cref="ApiError"/> format, error code "invalid_request".
    /// </summary>
    /// <param name="services">The application's service collection.</param>
    /// <returns>The same collection, for chaining.</returns>
    public static IServiceCollection AddSharedErrorFormat(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var problems = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .Select(entry =>
                    {
                        var field = string.IsNullOrEmpty(entry.Key) ? "request" : entry.Key;
                        return $"{field}: {entry.Value!.Errors[0].ErrorMessage}";
                    });

                var error = ApiError.Create(context.HttpContext, "invalid_request", string.Join(" ", problems));
                return new BadRequestObjectResult(error);
            };
        });
        return services;
    }
}
