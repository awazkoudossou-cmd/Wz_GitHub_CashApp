using CashApp.Application.Common.Exceptions;
using CashApp.Application.Common.Models;
using System.Net;
using System.Text.Json;
using AppValidationException = CashApp.Application.Common.Exceptions.ValidationException;
using FluentValidationException = FluentValidation.ValidationException;

namespace CashApp.Api.Middleware;

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
        catch (FluentValidationException ex)
        {
            var details = ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}");
            await WriteAsync(context, HttpStatusCode.BadRequest, "VALIDATION_ERROR", ex.Message, details);
        }
        catch (AppValidationException ex)
        {
            var details = ex.Errors.SelectMany(kv => kv.Value.Select(v => $"{kv.Key}: {v}"));
            await WriteAsync(context, HttpStatusCode.BadRequest, "VALIDATION_ERROR", ex.Message, details);
        }
        catch (NotFoundException ex)
        {
            await WriteAsync(context, HttpStatusCode.NotFound, "NOT_FOUND", ex.Message);
        }
        catch (ForbiddenException ex)
        {
            await WriteAsync(context, HttpStatusCode.Forbidden, "FORBIDDEN", ex.Message);
        }
        catch (BusinessRuleException ex)
        {
            await WriteAsync(context, HttpStatusCode.UnprocessableEntity, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "Une erreur interne est survenue.");
        }
    }

    private static async Task WriteAsync(HttpContext ctx, HttpStatusCode status, string code, string message, IEnumerable<string>? details = null)
    {
        ctx.Response.StatusCode = (int)status;
        ctx.Response.ContentType = "application/json";
        var combined = $"[{code}] {message}";
        var body = ApiResponse<object>.Fail(combined, details?.ToList());
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
