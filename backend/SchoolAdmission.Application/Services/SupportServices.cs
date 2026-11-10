using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Application.Services;

public class AuditService : IAuditService
{
    private readonly IAppDbContext _db;
    public AuditService(IAppDbContext db) => _db = db;

    public void Log(string actorType, string? actorId, string action, string entityType, Guid? entityId, object? details = null)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorType = actorType,
            ActorId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details is null ? null : JsonSerializer.Serialize(details),
            CreatedAt = DateTime.UtcNow
        });
    }
}

public record SeatCounts(int Held, int Confirmed);

/// <summary>Database-derived seat statistics (never cached in memory).</summary>
public class SeatAvailabilityService
{
    private readonly IAppDbContext _db;
    public SeatAvailabilityService(IAppDbContext db) => _db = db;

    public async Task<Dictionary<Guid, SeatCounts>> GetCountsAsync(IReadOnlyCollection<Guid>? classIds, DateTime now, CancellationToken ct)
    {
        var query = _db.Holds.AsNoTracking()
            .Where(h => h.Status == HoldStatus.Confirmed || (h.Status == HoldStatus.Held && h.ExpiresAt > now));
        if (classIds is not null)
            query = query.Where(h => classIds.Contains(h.ClassId));

        var rows = await query
            .GroupBy(h => new { h.ClassId, h.Status })
            .Select(g => new { g.Key.ClassId, g.Key.Status, Count = g.Count() })
            .ToListAsync(ct);

        var result = new Dictionary<Guid, SeatCounts>();
        foreach (var row in rows)
        {
            result.TryGetValue(row.ClassId, out var current);
            current ??= new SeatCounts(0, 0);
            result[row.ClassId] = row.Status == HoldStatus.Held
                ? current with { Held = row.Count }
                : current with { Confirmed = row.Count };
        }
        return result;
    }

    public async Task<SeatCounts> GetCountsForClassAsync(Guid classId, DateTime now, CancellationToken ct)
    {
        var held = await _db.Holds.CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt > now, ct);
        var confirmed = await _db.Holds.CountAsync(h => h.ClassId == classId && h.Status == HoldStatus.Confirmed, ct);
        return new SeatCounts(held, confirmed);
    }
}

public record HoldOwner(Guid ClassId, string StudentId);

/// <summary>Read-only hold queries shared by the reservation, payment and admin services.</summary>
public class HoldReader
{
    private readonly IAppDbContext _db;
    public HoldReader(IAppDbContext db) => _db = db;

    public async Task<HoldOwner?> GetOwnerAsync(Guid holdId, CancellationToken ct)
    {
        return await _db.Holds.AsNoTracking()
            .Where(h => h.Id == holdId)
            .Select(h => new HoldOwner(h.ClassId, h.Student!.BirthRegistrationNumber))
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>Loads the hold for a student; 404 if missing, 403 if it belongs to someone else.</summary>
    public async Task<HoldOwner> RequireOwnedAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        var owner = await GetOwnerAsync(holdId, ct) ?? throw Domain.Exceptions.DomainException.NotFound("Application not found.");
        if (owner.StudentId != studentId)
            throw Domain.Exceptions.DomainException.Forbidden("This application belongs to another student.");
        return owner;
    }

    public IQueryable<AdmissionHold> Query() => _db.Holds.AsNoTracking()
        .Include(h => h.Class).Include(h => h.Student).Include(h => h.Payment);

    public async Task<HoldDto?> GetDtoAsync(Guid holdId, CancellationToken ct)
    {
        var hold = await Query().FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold is null) return null;
        var now = await _db.GetDatabaseTimeAsync(ct);
        return HoldMapper.ToDto(hold, now);
    }
}

public class ExpirationService : IExpirationService
{
    private readonly IAppDbContext _db;
    private readonly IAuditService _audit;

    public ExpirationService(IAppDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<int> ExpireOverdueForClassAsync(Guid classId, DateTime databaseNow, CancellationToken ct)
    {
        var overdue = await _db.Holds
            .Where(h => h.ClassId == classId && h.Status == HoldStatus.Held && h.ExpiresAt <= databaseNow)
            .ToListAsync(ct);

        foreach (var hold in overdue)
        {
            hold.Status = HoldStatus.Expired;
            _audit.Log("SYSTEM", null, "HOLD_EXPIRED", "admission_hold", hold.Id,
                new { classId, expiredAt = hold.ExpiresAt });
        }

        if (overdue.Count > 0)
            await _db.SaveChangesAsync(ct);
        return overdue.Count;
    }

    public async Task<int> ExpireAllOverdueAsync(CancellationToken ct)
    {
        var now = await _db.GetDatabaseTimeAsync(ct);
        var classIds = await _db.Holds.AsNoTracking()
            .Where(h => h.Status == HoldStatus.Held && h.ExpiresAt <= now)
            .Select(h => h.ClassId).Distinct().Take(50).ToListAsync(ct);

        var total = 0;
        foreach (var classId in classIds)
        {
            await using var tx = await _db.BeginSeatTransactionAsync(ct);
            // Same lock order as every other operation: class row first.
            var cls = await _db.LockClassAsync(classId, ct);
            if (cls is null) continue;
            var lockedNow = await _db.GetDatabaseTimeAsync(ct);
            total += await ExpireOverdueForClassAsync(classId, lockedNow, ct);
            await tx.CommitAsync(ct);
            _db.ChangeTracker.Clear();
        }
        return total;
    }
}
