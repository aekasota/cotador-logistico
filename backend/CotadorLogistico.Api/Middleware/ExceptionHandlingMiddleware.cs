using System.Text.Json;
using CotadorLogistico.Api.Controllers;
using CotadorLogistico.Core.Domain;

namespace CotadorLogistico.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
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
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var correlationId = Guid.NewGuid().ToString("N");

            var (statusCode, message) = ex switch
            {
                SupabaseAdminException admin => (StatusCodes.Status409Conflict, admin.Message),
                NoProfileException or InsufficientRoleException => (StatusCodes.Status403Forbidden, ex.Message),
                ArgumentException or FormatException => (StatusCodes.Status400BadRequest, "Requisição inválida."),
                _ => (StatusCodes.Status500InternalServerError, "Ocorreu um erro inesperado. Tente novamente.")
            };

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Erro não tratado [{CorrelationId}].", correlationId);
            else
                _logger.LogWarning("Erro de negócio [{CorrelationId}]: {Message}", correlationId, ex.Message);

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = message,
                correlationId
            }));
        }
    }
}
