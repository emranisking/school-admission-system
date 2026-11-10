using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Npgsql;
using Xunit;

// All DB-backed tests share one PostgreSQL database and truncate it between tests, so they must not run in parallel.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace SchoolAdmission.Tests.Infrastructure;

public static class TestDb
{
    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5433;Database=school_admission_test;Username=postgres;Password=postgres;Maximum Pool Size=50";

    public static bool IsAvailable()
    {
        try
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            return true;
        }
        catch { return false; }
    }

    public static void RequireDatabase()
        => Skip.IfNot(IsAvailable(), "No PostgreSQL test database reachable. Start one with: docker compose -f docker-compose.test.yml up -d");

    /// <summary>Clears all data but keeps the schema and the seeded admin.</summary>
    public static async Task ResetAsync()
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        try
        {
            await using var cmd = new NpgsqlCommand(
                "TRUNCATE payments, audit_logs, admission_holds, classes, students CASCADE", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P01") { /* schema not created yet */ }
    }

    public static async Task<T> ScalarAsync<T>(string sql, params (string, object)[] parameters)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        var result = await cmd.ExecuteScalarAsync();
        return (T)Convert.ChangeType(result!, typeof(T));
    }

    public static Task ExecAsync(string sql, params (string, object)[] parameters)
        => ScalarAsyncNoResult(sql, parameters);

    private static async Task ScalarAsyncNoResult(string sql, (string, object)[] parameters)
    {
        await using var conn = new NpgsqlConnection(ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(sql, conn);
        foreach (var (name, value) in parameters) cmd.Parameters.AddWithValue(name, value);
        await cmd.ExecuteNonQueryAsync();
    }
}

/// <summary>One in-process backend instance (own DI container, own background worker) on the shared test database.</summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly bool _worker;
    public ApiFactory(bool worker = false) => _worker = worker;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", TestDb.ConnectionString);
        builder.UseSetting("Jwt:Key", "test-key-test-key-test-key-test-key-1234");
        builder.UseSetting("Admin:Username", "admin");
        builder.UseSetting("Admin:Password", "Test@12345");
        builder.UseSetting("Worker:Enabled", _worker ? "true" : "false");
        builder.UseSetting("Worker:IntervalSeconds", "1");
        builder.UseSetting("Seed:DemoData", "false");
    }

    public async Task<HttpClient> AdminClientAsync()
    {
        var client = CreateClient();
        var res = await client.PostAsJsonAsync("/admin/auth/login", new { username = "admin", password = "Test@12345" });
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
        return client;
    }
}

public static class Api
{
    public static string StudentId(int n) => (10_000_000_000_000_000L + n).ToString();

    public static async Task<JsonElement> Json(this HttpResponseMessage res)
        => await res.Content.ReadFromJsonAsync<JsonElement>();

    public static async Task<Guid> CreateClassAsync(HttpClient admin, int seats, int holdSeconds = 600, string name = "Class 6")
    {
        var res = await admin.PostAsJsonAsync("/classes", new
        {
            name, academicYear = 2027, shift = "MORNING", totalSeats = seats, holdDurationSeconds = holdSeconds, admissionOpen = true
        });
        Assert.Equal(System.Net.HttpStatusCode.Created, res.StatusCode);
        return (await res.Json()).GetProperty("id").GetGuid();
    }

    public static Task<HttpResponseMessage> ReserveAsync(HttpClient client, Guid classId, string studentId)
        => Send(client, HttpMethod.Post, $"/classes/{classId}/holds", studentId);

    public static Task<HttpResponseMessage> ConfirmAsync(HttpClient client, Guid holdId, string studentId)
        => Send(client, HttpMethod.Post, $"/holds/{holdId}/confirm", studentId);

    public static Task<HttpResponseMessage> CancelAsync(HttpClient client, Guid holdId, string studentId)
        => Send(client, HttpMethod.Delete, $"/holds/{holdId}", studentId);

    public static Task<HttpResponseMessage> GetHoldAsync(HttpClient client, Guid holdId, string studentId)
        => Send(client, HttpMethod.Get, $"/holds/{holdId}", studentId);

    public static Task<HttpResponseMessage> Send(HttpClient client, HttpMethod method, string url, string studentId)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-Student-Id", studentId);
        return client.SendAsync(req);
    }

    /// <summary>Runs <paramref name="count"/> operations with bounded parallelism.</summary>
    public static async Task<List<T>> RunConcurrentAsync<T>(int count, int maxParallel, Func<int, Task<T>> op)
    {
        var gate = new SemaphoreSlim(maxParallel);
        var tasks = Enumerable.Range(0, count).Select(async i =>
        {
            await gate.WaitAsync();
            try { return await op(i); }
            finally { gate.Release(); }
        });
        return (await Task.WhenAll(tasks)).ToList();
    }

    /// <summary>The invariants from section 13.3 of the plan; call after every concurrency test.</summary>
    public static async Task AssertInvariantsAsync(Guid classId)
    {
        var total = await TestDb.ScalarAsync<int>("SELECT total_seats FROM classes WHERE id = @id", ("id", classId));
        var held = await TestDb.ScalarAsync<int>(
            "SELECT COUNT(*) FROM admission_holds WHERE class_id = @id AND status = 'HELD' AND expires_at > clock_timestamp()", ("id", classId));
        var confirmed = await TestDb.ScalarAsync<int>(
            "SELECT COUNT(*) FROM admission_holds WHERE class_id = @id AND status = 'CONFIRMED'", ("id", classId));
        Assert.True(held + confirmed <= total, $"Overselling: held {held} + confirmed {confirmed} > total {total}");

        var duplicates = await TestDb.ScalarAsync<int>(
            @"SELECT COUNT(*) FROM (SELECT student_id FROM admission_holds
                WHERE class_id = @id AND status IN ('HELD','CONFIRMED')
                GROUP BY student_id HAVING COUNT(*) > 1) d", ("id", classId));
        Assert.Equal(0, duplicates);

        var doublePaid = await TestDb.ScalarAsync<int>(
            @"SELECT COUNT(*) FROM (SELECT p.hold_id FROM payments p JOIN admission_holds h ON h.id = p.hold_id
                WHERE h.class_id = @id AND p.payment_status = 'SUCCESS' GROUP BY p.hold_id HAVING COUNT(*) > 1) d", ("id", classId));
        Assert.Equal(0, doublePaid);

        var confirmedWithoutPayment = await TestDb.ScalarAsync<int>(
            @"SELECT COUNT(*) FROM admission_holds h LEFT JOIN payments p ON p.hold_id = h.id
              WHERE h.class_id = @id AND h.status = 'CONFIRMED' AND p.id IS NULL", ("id", classId));
        Assert.Equal(0, confirmedWithoutPayment);
    }
}
