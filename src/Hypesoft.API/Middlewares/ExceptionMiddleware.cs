using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Hypesoft.Domain.Exceptions;

namespace Hypesoft.API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
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
            _logger.LogError(ex, "Unhandled exception at {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException   ex => (HttpStatusCode.NotFound,           "Resource Not Found",         ex.Message),
            DomainException     ex => (HttpStatusCode.BadRequest,         "Business Rule Violation",    ex.Message),
            ValidationException ex => (HttpStatusCode.UnprocessableEntity,"Validation Failed",          ex.Message),
            _                      => (HttpStatusCode.InternalServerError, "Internal Server Error",     "An unexpected error occurred.")
        };

        var problem = new ProblemDetails
        {
            Type     = $"https://httpstatuses.com/{(int)statusCode}",
            Title    = title,
            Status   = (int)statusCode,
            Detail   = detail,
            Instance = context.Request.Path
        };

        problem.Extensions["traceId"] = context.TraceIdentifier;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode  = (int)statusCode;

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return context.Response.WriteAsync(JsonSerializer.Serialize(problem, options));
    }
}
