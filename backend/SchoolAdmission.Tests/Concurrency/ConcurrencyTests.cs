using System.Net;
using SchoolAdmission.Tests.Infrastructure;
using Xunit;

namespace SchoolAdmission.Tests.Concurrency;

/// <summary>
/// Concurrency tests (plan section 13.3) against real PostgreSQL. Each test ends by verifying the invariants:
/// held + confirmed &lt;= total, one active application per student, one successful payment per hold.
/// </summary>
public class ConcurrencyTests : IAsyncLifetime
{
    private ApiFactory _api1 = null!;
    private ApiFactory? _api2;
    private HttpClient _admin = null!;

    public async Task InitializeAsync()
    {
        if (!TestDb.IsAvailable()) return;
        _api1 = new ApiFactory(worker: true);
        _admin = await _api1.AdminClientAsync();
        await TestDb.ResetAsync();
    }

    public Task DisposeAsync()
    {
        _api1?.Dispose();
        _api2?.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<(int created, int conflict, int other)> Tally(IEnumerable<HttpResponseMessage> responses)
    {
        int created = 0, conflict = 0, other = 0;
        foreach (var r in responses)
        {
            if (r.StatusCode == HttpStatusCode.Created) created++;
            else if (r.StatusCode == HttpStatusCode.Conflict) conflict++;
            else other++;
        }
        await Task.CompletedTask;
        return (created, conflict, other);
    }

    private async Task RunOverbookingScenario(int requests, int seats, HttpClient[] clients)
    {
        var classId = await Api.CreateClassAsync(_admin, seats);
        var responses = await Api.RunConcurrentAsync(requests, 200, i =>
            Api.ReserveAsync(clients[i % clients.Length], classId, Api.StudentId(i + 1)));

        var (created, conflict, other) = await Tally(responses);
        Assert.Equal(seats, created);
        Assert.Equal(requests - seats, conflict);
        Assert.Equal(0, other);
        Assert.Equal(seats, await TestDb.ScalarAsync<int>(
            "SELECT COUNT(*) FROM admission_holds WHERE class_id = @id AND status = 'HELD'", ("id", classId)));
        await Api.AssertInvariantsAsync(classId);
    }

    [SkippableFact] // Test A
    public async Task A_1000_requests_for_120_seats()
    {
        TestDb.RequireDatabase();
        await RunOverbookingScenario(1000, 120, new[] { _api1.CreateClient() });
    }

    [SkippableFact] // Test B
    public async Task B_500_requests_for_10_seats()
    {
        TestDb.RequireDatabase();
        await RunOverbookingScenario(500, 10, new[] { _api1.CreateClient() });
    }

    [SkippableFact] // Test C
    public async Task C_100_requests_for_1_seat()
    {
        TestDb.RequireDatabase();
        await RunOverbookingScenario(100, 1, new[] { _api1.CreateClient() });
    }

    [SkippableFact] // Test D: two independent backend instances (own DI container + worker) sharing one database
    public async Task D_two_backend_instances_same_class()
    {
        TestDb.RequireDatabase();
        _api2 = new ApiFactory(worker: true);
        await RunOverbookingScenario(600, 50, new[] { _api1.CreateClient(), _api2.CreateClient() });
    }

    [SkippableFact] // Test E
    public async Task E_simultaneous_confirmation_and_expiration()
    {
        TestDb.RequireDatabase();
        // Short holds + running workers on both instances; confirmations race the deadline.
        _api2 = new ApiFactory(worker: true);
        var clients = new[] { _api1.CreateClient(), _api2.CreateClient() };
        var classId = await Api.CreateClassAsync(_admin, 20, holdSeconds: 2);

        var holds = new List<(Guid id, string student)>();
        for (var i = 0; i < 20; i++)
        {
            var student = Api.StudentId(i + 1);
            var res = await Api.ReserveAsync(clients[0], classId, student);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            holds.Add(((await res.Json()).GetProperty("holdId").GetGuid(), student));
        }

        await Task.Delay(TimeSpan.FromMilliseconds(1850)); // right at the deadline
        var results = await Api.RunConcurrentAsync(holds.Count * 2, 40, i =>
        {
            var (id, student) = holds[i % holds.Count];
            return Api.ConfirmAsync(clients[i % 2], id, student);
        });

        foreach (var r in results)
            Assert.True(r.StatusCode is HttpStatusCode.OK or HttpStatusCode.Conflict, $"Unexpected {r.StatusCode}");

        await Task.Delay(TimeSpan.FromSeconds(3)); // let the workers settle
        foreach (var (id, _) in holds)
        {
            var status = await TestDb.ScalarAsync<string>("SELECT status FROM admission_holds WHERE id = @id", ("id", id));
            var payments = await TestDb.ScalarAsync<int>("SELECT COUNT(*) FROM payments WHERE hold_id = @id", ("id", id));
            Assert.True(status is "CONFIRMED" or "EXPIRED", $"Hold ended as {status}");
            Assert.Equal(status == "CONFIRMED" ? 1 : 0, payments);
        }
        await Api.AssertInvariantsAsync(classId);
    }

    [SkippableFact] // Test F
    public async Task F_simultaneous_cancellation_and_new_reservation()
    {
        TestDb.RequireDatabase();
        var client = _api1.CreateClient();
        for (var round = 0; round < 15; round++)
        {
            var classId = await Api.CreateClassAsync(_admin, 1, name: $"Round {round}");
            var holder = Api.StudentId(1000 + round * 2);
            var newcomer = Api.StudentId(1001 + round * 2);
            var holdId = (await (await Api.ReserveAsync(client, classId, holder)).Json()).GetProperty("holdId").GetGuid();

            var cancel = Api.CancelAsync(client, holdId, holder);
            var reserve = Api.ReserveAsync(client, classId, newcomer);
            await Task.WhenAll(cancel, reserve);

            Assert.Equal(HttpStatusCode.OK, cancel.Result.StatusCode);
            // Either the newcomer won the lock first (seat still held -> 409) or after the cancel (-> 201). Never both seats.
            Assert.True(reserve.Result.StatusCode is HttpStatusCode.Created or HttpStatusCode.Conflict);
            await Api.AssertInvariantsAsync(classId);
        }
    }
}
