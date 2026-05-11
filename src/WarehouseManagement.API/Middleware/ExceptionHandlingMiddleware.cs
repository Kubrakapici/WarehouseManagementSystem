using System.Net;
using System.Text.Json;
using FluentValidation;
using WarehouseManagement.Application.Common;

namespace WarehouseManagement.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ApiResponse.Fail("Doğrulama hatası", ex.Errors.Select(e => e.ErrorMessage)));
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteAsync(context, HttpStatusCode.Forbidden, ApiResponse.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await WriteAsync(context, HttpStatusCode.BadRequest, ApiResponse.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İşlenmeyen hata");
            await WriteAsync(context, HttpStatusCode.InternalServerError, ApiResponse.Fail("Beklenmeyen bir hata oluştu."));
        }
    }

    private static Task WriteAsync(HttpContext context, HttpStatusCode code, ApiResponse response)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        return context.Response.WriteAsync(JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}
