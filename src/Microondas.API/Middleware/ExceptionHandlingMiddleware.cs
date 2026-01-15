using System.Text.Json;
using Microondas.API.Exceptions;

namespace Microondas.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _logPath;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
        var baseDir = AppContext.BaseDirectory;
        _logPath = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(_logPath);
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log completo (stack + inner)
        var logFile = Path.Combine(_logPath, $"exceptions-{DateTime.UtcNow:yyyyMMdd}.log");
        var logText = $"[{DateTime.UtcNow:O}] {exception.GetType().FullName}: {exception.Message}\nStack: {exception.StackTrace}\n";
        if (exception.InnerException != null)
            logText += $"Inner: {exception.InnerException.GetType().FullName}: {exception.InnerException.Message}\nInnerStack: {exception.InnerException.StackTrace}\n";
        await File.AppendAllTextAsync(logFile, logText + "\n");

        context.Response.ContentType = "application/json";

        if (exception is BusinessException bex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var payload = new StandardError(bex.Code ?? "business_rule", bex.Message);
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
            return;
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        var standard = new StandardError("server_error", "Erro interno no servidor");
        await context.Response.WriteAsync(JsonSerializer.Serialize(standard));
    }
}