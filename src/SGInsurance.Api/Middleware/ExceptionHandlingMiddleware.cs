using System.Net;
using System.Text.Json;
using FluentValidation;
using SGInsurance.Application.Common;

namespace SGInsurance.Api.Middleware;

/// <summary>
/// Catches unhandled exceptions anywhere downstream and converts them into the
/// standard { success, data, message, errors } envelope with an appropriate
/// status code, instead of leaking a raw stack trace to the client.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception ex)
    {
        var (statusCode, message, errors) = ex switch
        {
            ValidationException validationEx => (
                HttpStatusCode.BadRequest,
                "Validation failed.",
                validationEx.Errors.Select(e => e.ErrorMessage).ToList()),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message, new List<string>()),
            KeyNotFoundException => (HttpStatusCode.NotFound, ex.Message, new List<string>()),
            InvalidOperationException => (HttpStatusCode.BadRequest, ex.Message, new List<string>()),
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message, new List<string>()),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.", new List<string>())
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(ex, "Handled exception ({StatusCode}) processing {Method} {Path}", (int)statusCode, context.Request.Method, context.Request.Path);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = ApiResponse<object?>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}

public static class ExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseSGInsuranceExceptionHandling(this IApplicationBuilder app) =>
        app.UseMiddleware<ExceptionHandlingMiddleware>();
}
