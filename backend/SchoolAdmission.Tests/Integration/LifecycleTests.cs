using System.Net;
using System.Net.Http.Json;
using SchoolAdmission.Tests.Infrastructure;
using Xunit;

namespace SchoolAdmission.Tests.Integration;

/// <summary>Integration tests against a real PostgreSQL database (never EF's in-memory provider).</summary>
public class LifecycleTests : IAsyncLifetime
{
    private ApiFactory _api = null!;
    private HttpClient _admin = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        if (!TestDb.IsAvailable()) return;
        _api = new ApiFactory(worker: false);
        _client = _api.CreateClient(); // first request boots the host and applies migrations
        _admin = await _api.AdminClientAsync();
        await TestDb.ResetAsync();
    }

    public Task DisposeAsync()
    {
        _api?.Dispose();
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task Create_class_is_persisted_and_listed()
    {
        TestDb.RequireDatabase();
        var id = await Api.CreateClassAsync(_admin, 120, 1800);
        var cls = await (await _client.GetAsync($"/classes/{id}")).Json();
        Assert.Equal(120, cls.GetProperty("availableSeats").GetInt32());
        Assert.Equal(1800, cls.GetProperty("holdDurationSeconds").GetInt32());
        Assert.Equal("Class 6 - Morning Shift", cls.GetProperty("displayName").GetString());
        Assert.Equal(1, await TestDb.ScalarAsync<int>("SELECT COUNT(*) FROM classes WHERE id = @id", ("id", id)));
    }

    [SkippableFact]
    public async Task Creating_a_class_requires_admin()
    {
        TestDb.RequireDatabase();
        var res = await _client.PostAsJsonAsync("/classes", new { name = "X", academicYear = 2027, shift = "DAY", totalSeats = 5 });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [SkippableFact]
    public async Task Reserve_stores_expiry_and_returns_it()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5, 1800);
        var res = await Api.ReserveAsync(_client, classId, Api.StudentId(1));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var hold = await res.Json();
        Assert.Equal("HELD", hold.GetProperty("status").GetString());
        var created = hold.GetProperty("createdAt").GetDateTime();
        var expires = hold.GetProperty("expiresAt").GetDateTime();
        Assert.Equal(1800, (int)Math.Round((expires - created).TotalSeconds));
        Assert.Equal(1800, hold.GetProperty("remainingSeconds").GetInt32());
    }

    [SkippableFact]
    public async Task Invalid_student_header_is_rejected()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        var res = await Api.ReserveAsync(_client, classId, "123");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [SkippableFact]
    public async Task Duplicate_application_is_rejected()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        Assert.Equal(HttpStatusCode.Created, (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).StatusCode);
        var second = await Api.ReserveAsync(_client, classId, Api.StudentId(1));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("DUPLICATE_APPLICATION", (await second.Json()).GetProperty("error").GetProperty("code").GetString());
    }

    [SkippableFact]
    public async Task Last_seat_cannot_be_taken_twice()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 1);
        Assert.Equal(HttpStatusCode.Created, (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).StatusCode);
        var second = await Api.ReserveAsync(_client, classId, Api.StudentId(2));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("SEAT_UNAVAILABLE", (await second.Json()).GetProperty("error").GetProperty("code").GetString());
    }

    [SkippableFact]
    public async Task Confirm_records_one_payment_and_is_idempotent()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        var holdId = (await (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).Json()).GetProperty("holdId").GetGuid();

        var first = await Api.ConfirmAsync(_client, holdId, Api.StudentId(1));
        var second = await Api.ConfirmAsync(_client, holdId, Api.StudentId(1));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var a = await first.Json();
        var b = await second.Json();
        Assert.Equal("CONFIRMED", a.GetProperty("status").GetString());
        Assert.Equal(a.GetProperty("payment").GetProperty("transactionReference").GetString(),
                     b.GetProperty("payment").GetProperty("transactionReference").GetString());
        Assert.Equal(1, await TestDb.ScalarAsync<int>("SELECT COUNT(*) FROM payments WHERE hold_id = @id", ("id", holdId)));

        var cls = await (await _client.GetAsync($"/classes/{classId}")).Json();
        Assert.Equal(1, cls.GetProperty("confirmedSeats").GetInt32());
        Assert.Equal(4, cls.GetProperty("availableSeats").GetInt32());
    }

    [SkippableFact]
    public async Task Another_student_cannot_confirm_or_cancel()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        var holdId = (await (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).Json()).GetProperty("holdId").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await Api.ConfirmAsync(_client, holdId, Api.StudentId(2))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Api.CancelAsync(_client, holdId, Api.StudentId(2))).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await Api.GetHoldAsync(_client, holdId, Api.StudentId(2))).StatusCode);
    }

    [SkippableFact]
    public async Task Cancel_releases_the_seat_and_confirmed_cannot_be_cancelled()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 1);
        var holdId = (await (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).Json()).GetProperty("holdId").GetGuid();
        var cancelled = await Api.CancelAsync(_client, holdId, Api.StudentId(1));
        Assert.Equal("CANCELLED", (await cancelled.Json()).GetProperty("status").GetString());

        var other = await Api.ReserveAsync(_client, classId, Api.StudentId(2));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
        var otherHold = (await other.Json()).GetProperty("holdId").GetGuid();
        await Api.ConfirmAsync(_client, otherHold, Api.StudentId(2));
        Assert.Equal(HttpStatusCode.Conflict, (await Api.CancelAsync(_client, otherHold, Api.StudentId(2))).StatusCode);
    }

    [SkippableFact]
    public async Task Expired_hold_cannot_be_confirmed_and_seat_is_reusable()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 1, holdSeconds: 1);
        var holdId = (await (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).Json()).GetProperty("holdId").GetGuid();

        await Task.Delay(TimeSpan.FromMilliseconds(1600));

        // reads already report EXPIRED even before anything has processed it
        var view = await (await Api.GetHoldAsync(_client, holdId, Api.StudentId(1))).Json();
        Assert.Equal("EXPIRED", view.GetProperty("status").GetString());

        var confirm = await Api.ConfirmAsync(_client, holdId, Api.StudentId(1));
        Assert.Equal(HttpStatusCode.Conflict, confirm.StatusCode);
        Assert.Equal("RESERVATION_EXPIRED", (await confirm.Json()).GetProperty("error").GetProperty("code").GetString());
        Assert.Equal("EXPIRED", await TestDb.ScalarAsync<string>("SELECT status FROM admission_holds WHERE id = @id", ("id", holdId)));
        Assert.Equal(0, await TestDb.ScalarAsync<int>("SELECT COUNT(*) FROM payments WHERE hold_id = @id", ("id", holdId)));

        var other = await Api.ReserveAsync(_client, classId, Api.StudentId(2));
        Assert.Equal(HttpStatusCode.Created, other.StatusCode);
    }

    [SkippableFact]
    public async Task New_reservation_expires_overdue_holds_without_the_worker()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 1, holdSeconds: 1);
        await Api.ReserveAsync(_client, classId, Api.StudentId(1));
        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        Assert.Equal(HttpStatusCode.Created, (await Api.ReserveAsync(_client, classId, Api.StudentId(2))).StatusCode);
        Assert.Equal(1, await TestDb.ScalarAsync<int>(
            "SELECT COUNT(*) FROM admission_holds WHERE class_id = @id AND status = 'EXPIRED'", ("id", classId)));
    }

    [SkippableFact]
    public async Task Capacity_cannot_drop_below_committed_seats()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        for (var i = 1; i <= 3; i++) await Api.ReserveAsync(_client, classId, Api.StudentId(i));

        var tooLow = await _admin.PatchAsJsonAsync($"/admin/classes/{classId}/capacity", new { totalSeats = 2 });
        Assert.Equal(HttpStatusCode.Conflict, tooLow.StatusCode);
        Assert.Equal("CAPACITY_BELOW_COMMITMENTS", (await tooLow.Json()).GetProperty("error").GetProperty("code").GetString());

        var ok = await _admin.PatchAsJsonAsync($"/admin/classes/{classId}/capacity", new { totalSeats = 3 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [SkippableFact]
    public async Task Changing_hold_duration_only_affects_future_reservations()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5, 1800);
        var before = await (await Api.ReserveAsync(_client, classId, Api.StudentId(1))).Json();

        var patch = await _admin.PatchAsJsonAsync($"/admin/classes/{classId}/hold-duration", new { holdDurationSeconds = 900 });
        Assert.Equal(HttpStatusCode.OK, patch.StatusCode);

        var after = await (await Api.ReserveAsync(_client, classId, Api.StudentId(2))).Json();
        var beforeSpan = (before.GetProperty("expiresAt").GetDateTime() - before.GetProperty("createdAt").GetDateTime()).TotalSeconds;
        var afterSpan = (after.GetProperty("expiresAt").GetDateTime() - after.GetProperty("createdAt").GetDateTime()).TotalSeconds;
        Assert.Equal(1800, (int)Math.Round(beforeSpan));
        Assert.Equal(900, (int)Math.Round(afterSpan));
    }

    [SkippableFact]
    public async Task Closed_admission_rejects_applications()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        await _admin.PatchAsJsonAsync($"/admin/classes/{classId}/admission-status", new { admissionOpen = false });
        var res = await Api.ReserveAsync(_client, classId, Api.StudentId(1));
        Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
        Assert.Equal("ADMISSION_CLOSED", (await res.Json()).GetProperty("error").GetProperty("code").GetString());
    }

    [SkippableFact]
    public async Task Student_history_and_admin_dashboard_work()
    {
        TestDb.RequireDatabase();
        var classId = await Api.CreateClassAsync(_admin, 5);
        await Api.ReserveAsync(_client, classId, Api.StudentId(1));

        var mine = await (await Api.Send(_client, HttpMethod.Get, "/me/holds", Api.StudentId(1))).Json();
        Assert.Equal(1, mine.GetArrayLength());

        var dash = await (await _admin.GetAsync("/admin/dashboard")).Json();
        Assert.Equal(1, dash.GetProperty("heldSeats").GetInt32());
        Assert.Equal(4, dash.GetProperty("availableSeats").GetInt32());

        var apps = await (await _admin.GetAsync($"/admin/applications?classId={classId}&status=HELD")).Json();
        Assert.Equal(1, apps.GetProperty("total").GetInt32());
    }
}
