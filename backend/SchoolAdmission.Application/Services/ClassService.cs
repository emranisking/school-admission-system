using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public class ClassService : IClassService
{
    private readonly IAppDbContext _db;
    private readonly IExpirationService _expiration;
    private readonly IAuditService _audit;
    private readonly SeatAvailabilityService _seats;
    private readonly AdmissionOptions _options;

    public ClassService(IAppDbContext db, IExpirationService expiration, IAuditService audit,
        SeatAvailabilityService seats, IOptions<AdmissionOptions> options)
    {
        _db = db;
        _expiration = expiration;
        _audit = audit;
        _seats = seats;
        _options = options.Value;
    }

    public async Task<ClassDto> CreateAsync(CreateClassRequest request, string actor, CancellationToken ct)
    {
        var name = Validation.RequireName(request.Name);
        var year = Validation.RequireAcademicYear(request.AcademicYear);
        var shift = Validation.NormalizeShift(request.Shift);
        var seats = Validation.RequireSeats(request.TotalSeats);
        var duration = Validation.RequireHoldDuration(request.HoldDurationSeconds ?? _options.DefaultHoldDurationSeconds);

        var now = await _db.GetDatabaseTimeAsync(ct);
        var cls = new AdmissionClass
        {
            Id = Guid.NewGuid(),
            Name = name,
            AcademicYear = year,
            Shift = shift,
            TotalSeats = seats,
            HoldDurationSeconds = duration,
            AdmissionOpen = request.AdmissionOpen ?? true,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Classes.Add(cls);
        _audit.Log("ADMIN", actor, "CLASS_CREATED", "class", cls.Id,
            new { cls.Name, cls.AcademicYear, cls.Shift, cls.TotalSeats, cls.HoldDurationSeconds });
        await _db.SaveChangesAsync(ct);
        return ClassMapper.ToDto(cls, 0, 0);
    }

    public async Task<IReadOnlyList<ClassDto>> ListAsync(CancellationToken ct)
    {
        var now = await _db.GetDatabaseTimeAsync(ct);
        var classes = await _db.Classes.AsNoTracking()
            .OrderByDescending(c => c.AcademicYear).ThenBy(c => c.Name).ThenBy(c => c.Shift)
            .ToListAsync(ct);
        var counts = await _seats.GetCountsAsync(null, now, ct);
        return classes.Select(c =>
        {
            counts.TryGetValue(c.Id, out var sc);
            return ClassMapper.ToDto(c, sc?.Held ?? 0, sc?.Confirmed ?? 0);
        }).ToList();
    }

    public async Task<ClassDto> GetAsync(Guid classId, CancellationToken ct)
    {
        var cls = await _db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == classId, ct)
                  ?? throw DomainException.NotFound("Class not found.");
        var now = await _db.GetDatabaseTimeAsync(ct);
        var counts = await _seats.GetCountsForClassAsync(classId, now, ct);
        return ClassMapper.ToDto(cls, counts.Held, counts.Confirmed);
    }

    public async Task<ClassDto> UpdateDetailsAsync(Guid classId, UpdateClassRequest request, string actor, CancellationToken ct)
    {
        await using (var tx = await _db.BeginSeatTransactionAsync(ct))
        {
            var cls = await _db.LockClassAsync(classId, ct) ?? throw DomainException.NotFound("Class not found.");
            if (request.Name is not null) cls.Name = Validation.RequireName(request.Name);
            if (request.AcademicYear is not null) cls.AcademicYear = Validation.RequireAcademicYear(request.AcademicYear);
            if (request.Shift is not null) cls.Shift = Validation.NormalizeShift(request.Shift);
            cls.UpdatedAt = await _db.GetDatabaseTimeAsync(ct);
            _audit.Log("ADMIN", actor, "CLASS_UPDATED", "class", cls.Id, new { cls.Name, cls.AcademicYear, cls.Shift });
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        return await GetAsync(classId, ct);
    }

    public async Task<ClassDto> UpdateCapacityAsync(Guid classId, UpdateCapacityRequest request, string actor, CancellationToken ct)
    {
        var newTotal = Validation.RequireSeats(request.TotalSeats);
        DomainException? deferred = null;

        await using (var tx = await _db.BeginSeatTransactionAsync(ct))
        {
            var cls = await _db.LockClassAsync(classId, ct) ?? throw DomainException.NotFound("Class not found.");
            var now = await _db.GetDatabaseTimeAsync(ct);
            await _expiration.ExpireOverdueForClassAsync(classId, now, ct);
            var counts = await _seats.GetCountsForClassAsync(classId, now, ct);
            var committed = counts.Held + counts.Confirmed;

            if (newTotal < committed)
            {
                deferred = DomainException.Conflict("CAPACITY_BELOW_COMMITMENTS",
                    $"Capacity cannot be lower than the {committed} seats already held or confirmed.");
            }
            else
            {
                var old = cls.TotalSeats;
                cls.TotalSeats = newTotal;
                cls.UpdatedAt = now;
                _audit.Log("ADMIN", actor, "CAPACITY_CHANGED", "class", cls.Id, new { from = old, to = newTotal });
                await _db.SaveChangesAsync(ct);
            }
            await tx.CommitAsync(ct); // commits any expirations processed above either way
        }

        if (deferred is not null) throw deferred;
        return await GetAsync(classId, ct);
    }

    public async Task<HoldDurationResponse> UpdateHoldDurationAsync(Guid classId, UpdateHoldDurationRequest request, string actor, CancellationToken ct)
    {
        var seconds = Validation.RequireHoldDuration(request.HoldDurationSeconds);
        await using var tx = await _db.BeginSeatTransactionAsync(ct);
        var cls = await _db.LockClassAsync(classId, ct) ?? throw DomainException.NotFound("Class not found.");
        var old = cls.HoldDurationSeconds;
        cls.HoldDurationSeconds = seconds; // applies to future reservations only; existing expires_at is untouched
        cls.UpdatedAt = await _db.GetDatabaseTimeAsync(ct);
        _audit.Log("ADMIN", actor, "HOLD_DURATION_CHANGED", "class", cls.Id, new { from = old, to = seconds });
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new HoldDurationResponse(classId, seconds, "Reservation duration updated successfully");
    }

    public async Task<ClassDto> UpdateAdmissionStatusAsync(Guid classId, UpdateAdmissionStatusRequest request, string actor, CancellationToken ct)
    {
        if (request.AdmissionOpen is null)
            throw DomainException.Validation("admissionOpen is required.");

        await using (var tx = await _db.BeginSeatTransactionAsync(ct))
        {
            var cls = await _db.LockClassAsync(classId, ct) ?? throw DomainException.NotFound("Class not found.");
            cls.AdmissionOpen = request.AdmissionOpen.Value;
            cls.UpdatedAt = await _db.GetDatabaseTimeAsync(ct);
            _audit.Log("ADMIN", actor, cls.AdmissionOpen ? "ADMISSION_OPENED" : "ADMISSION_CLOSED", "class", cls.Id);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        }
        return await GetAsync(classId, ct);
    }
}
