using System.Text.Json;
using Kaan.SecurityPlatform.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace Kaan.SecurityPlatform.Api.Infrastructure.Middleware;

public sealed class ProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ProblemDetailsMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ProblemDetailsMiddleware(
        RequestDelegate next,
        ILogger<ProblemDetailsMiddleware> logger,
        IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (KaanApplicationException ex)
        {
            _logger.LogWarning(ex, "Uygulama hatası: {Code}", ex.ErrorCode);
            await WriteAsync(context, MapToStatus(ex), ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Beklenmeyen hata");
            var detail = _env.IsDevelopment() ? ex.ToString() : "Beklenmeyen bir hata oluştu.";
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "internal_error", detail);
        }
    }

    private static int MapToStatus(KaanApplicationException ex) => ex switch
    {
        NotFoundException => StatusCodes.Status404NotFound,
        ForbiddenAccessException => StatusCodes.Status403Forbidden,
        MembershipNotApprovedException => StatusCodes.Status403Forbidden,
        DomainNotVerifiedException => StatusCodes.Status422UnprocessableEntity,
        UnsafeScanTargetException => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status400BadRequest
    };

    private static async Task WriteAsync(HttpContext context, int statusCode, string code, string detail)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = $"https://kaansecurity.local/errors/{code}",
            Title = code,
            Detail = detail,
            Status = statusCode,
            Instance = context.Request.Path
        };

        problem.Extensions["errorCode"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        await JsonSerializer.SerializeAsync(context.Response.Body, problem, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}
