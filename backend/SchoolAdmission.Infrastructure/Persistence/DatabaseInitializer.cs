using System.Net.Sockets;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence;

/// <summary>
/// Runs before the app accepts traffic: applies embedded SQL migrations (guarded by a PostgreSQL advisory lock
/// so several backend instances can start together), seeds the admin account and optional demo data.
/// </summary>
public static class DatabaseInitializer
{
    private const long MigrationLockKey = 727_274_001;

    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");
        var config = services.GetRequiredService<IConfiguration>();
        var connectionString = config.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");

        await MigrateAsync(connectionString, logger, ct);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        await SeedAdminAsync(db, hasher, config, logger, ct);
        if (config.GetValue<bool>("Seed:DemoData"))
            await SeedDemoDataAsync(db, logger, ct);
    }

    private static async Task MigrateAsync(string connectionString, ILogger logger, CancellationToken ct)
    {
        const int maxAttempts = 30;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync(ct);
                await RunScriptsAsync(conn, logger, ct);
                return;
            }
            catch (Exception ex) when (attempt < maxAttempts && (ex is NpgsqlException or SocketException))
            {
                logger.LogWarning("Database not ready (attempt {Attempt}/{Max}): {Message}", attempt, maxAttempts, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    private static async Task RunScriptsAsync(NpgsqlConnection conn, ILogger logger, CancellationToken ct)
    {
        await Exec(conn, $"SELECT pg_advisory_lock({MigrationLockKey})", ct);
        try
        {
            await Exec(conn,
                "CREATE TABLE IF NOT EXISTS schema_migrations (version TEXT PRIMARY KEY, applied_at TIMESTAMPTZ NOT NULL DEFAULT NOW())", ct);

            var assembly = typeof(DatabaseInitializer).Assembly;
            var scripts = assembly.GetManifestResourceNames()
                .Where(n => n.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.Ordinal);

            foreach (var resource in scripts)
            {
                var version = resource.Split('.')[^2]; // e.g. 001_init
                await using (var check = new NpgsqlCommand("SELECT 1 FROM schema_migrations WHERE version = @v", conn))
                {
                    check.Parameters.AddWithValue("v", version);
                    if (await check.ExecuteScalarAsync(ct) is not null) continue;
                }

                using var stream = assembly.GetManifestResourceStream(resource)!;
                using var reader = new StreamReader(stream);
                var sql = await reader.ReadToEndAsync(ct);

                await using var tx = await conn.BeginTransactionAsync(ct);
                await using (var run = new NpgsqlCommand(sql, conn, tx)) await run.ExecuteNonQueryAsync(ct);
                await using (var mark = new NpgsqlCommand("INSERT INTO schema_migrations (version) VALUES (@v)", conn, tx))
                {
                    mark.Parameters.AddWithValue("v", version);
                    await mark.ExecuteNonQueryAsync(ct);
                }
                await tx.CommitAsync(ct);
                logger.LogInformation("Applied database migration {Version}", version);
            }
        }
        finally
        {
            await Exec(conn, $"SELECT pg_advisory_unlock({MigrationLockKey})", ct);
        }
    }

    private static async Task Exec(NpgsqlConnection conn, string sql, CancellationToken ct)
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private static async Task SeedAdminAsync(AppDbContext db, IPasswordHasher hasher, IConfiguration config, ILogger logger, CancellationToken ct)
    {
        var username = config["Admin:Username"] ?? "admin";
        var password = config["Admin:Password"] ?? "Admin@12345";

        if (await db.Admins.AnyAsync(a => a.Username == username, ct)) return;

        await db.Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO admins (id, username, password_hash, created_at)
               VALUES ({Guid.NewGuid()}, {username}, {hasher.Hash(password)}, clock_timestamp())
               ON CONFLICT (username) DO NOTHING", ct);
        logger.LogInformation("Seeded admin account '{Username}'", username);
    }

    private static async Task SeedDemoDataAsync(AppDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.Classes.AnyAsync(ct)) return;
        var now = DateTime.UtcNow;

        AdmissionClass Make(string name, string shift, int seats, int holdSeconds) => new()
        {
            Id = Guid.NewGuid(), Name = name, Shift = shift, AcademicYear = 2027, TotalSeats = seats,
            HoldDurationSeconds = holdSeconds, AdmissionOpen = true, CreatedAt = now, UpdatedAt = now
        };

        db.Classes.AddRange(
            Make("Class 6", "MORNING", 120, 1800),
            Make("Class 6", "DAY", 80, 900),
            Make("Class 7", "MORNING", 60, 600),
            Make("Class 9 Science", "MORNING", 40, 300));
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Seeded demo classes");
    }
}
