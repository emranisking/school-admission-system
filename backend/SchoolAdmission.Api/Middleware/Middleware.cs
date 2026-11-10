using System.Diagnostics;
using System.Text.Json;
using Npgsql;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
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
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            context.Response.StatusCode = 499; // client closed the request
        }
        catch (DomainException ex)
        {
            await Write(context, StatusFor(ex.Kind), ex.Code, ex.Message);
        }
        catch (Exception ex) when (FindPostgres(ex) is { } pg && IsTransient(pg))
        {
            _logger.LogWarning(ex, "Transient database condition {SqlState}", pg.SqlState);
            await Write(context, StatusCodes.Status503ServiceUnavailable, "BUSY",
                "The system is busy right now. Please try again in a moment.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR",
                "An unexpected error occurred.");
        }
    }

    private static int StatusFor(ErrorKind kind) => kind switch
    {
        ErrorKind.Validation => StatusCodes.Status400BadRequest,
        ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        ErrorKind.NotFound => StatusCodes.Status404NotFound,
        ErrorKind.Conflict => StatusCodes.Status409Conflict,
        ErrorKind.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static PostgresException? FindPostgres(Exception? ex)
    {
        for (; ex is not null; ex = ex.InnerException)
            if (ex is PostgresException pg) return pg;
        return null;
    }

    // 55P03 lock_timeout, 57014 statement_timeout, 40P01 deadlock, 40001 serialization failure
    private static bool IsTransient(PostgresException pg) => pg.SqlState is "55P03" or "57014" or "40P01" or "40001";

    private static async Task Write(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        if (status == StatusCodes.Status503ServiceUnavailable)
            context.Response.Headers["Retry-After"] = "1";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = new { code, message } }, Json));
    }
}

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("{Method} {Path} -> {Status} in {Elapsed} ms",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, sw.ElapsedMilliseconds);
        }
    }
}
