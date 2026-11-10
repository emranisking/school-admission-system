using SchoolAdmission.Api.Middleware;

namespace SchoolAdmission.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseSchoolAdmissionPipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseCors();
        app.UseJsonAuthErrors(); // must wrap authentication/authorization to see their 401/403
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    /// <summary>JSON body for 401/403 produced by the authorization middleware.</summary>
    public static IApplicationBuilder UseJsonAuthErrors(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            await next();
            if (context.Response.HasStarted || context.Response.ContentLength > 0) return;

            (string code, string message)? error = context.Response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => ("UNAUTHORIZED", "Authentication is required."),
                StatusCodes.Status403Forbidden => ("FORBIDDEN", "You do not have permission to do this."),
                _ => null
            };
            if (error is { } e)
            {
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = new { code = e.code, message = e.message } });
            }
        });
    }
}
