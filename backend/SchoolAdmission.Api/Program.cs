using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Extensions;
using SchoolAdmission.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiConventions();
builder.Services.AddSchoolAdmission(builder.Configuration);
builder.Services.AddAdminAuthentication(builder.Configuration);

var app = builder.Build();

// Controlled startup: apply migrations and seed BEFORE the server accepts traffic
// (hosted services such as the expiration worker start after this, when app.Run() is called).
await DatabaseInitializer.InitializeAsync(app.Services);

app.UseSchoolAdmissionPipeline();

app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
{
    var ok = await db.Database.CanConnectAsync(ct);
    return ok ? Results.Ok(new { status = "healthy" }) : Results.StatusCode(503);
});

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory<Program> in the integration tests.
public partial class Program { }
