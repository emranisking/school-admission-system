using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public class ReservationService : IReservationService
{
    private readonly IAppDbContext _db;
    private readonly IExpirationService _expiration;
    private readonly IAuditService _audit;
    private readonly SeatAvailabilityService _seats;
    private readonly HoldReader _reader;

    public ReservationService(IAppDbContext db, IExpirationService expiration, IAuditService audit,
        SeatAvailabilityService seats, HoldReader reader)
    {
        _db = db;
        _expiration = expiration;
        _audit = audit;
        _seats = seats;
        _reader = reader;
    }

    /// <summary>
    /// Reservation transaction (plan section 6.1):
    /// lock class row -> check open -> expire overdue holds -> duplicate check -> capacity check -> insert -> commit.
    /// Business rejections are raised AFTER commit so that expirations processed on the way are never rolled back.
    /// </summary>
    public async Task<HoldDto> ReserveAsync(Guid classId, string studentId, CancellationToken ct)
    {
        studentId = Validation.RequireStudentId(studentId);

        // Outside the seat transaction on purpose: avoids a student-row/class-row lock-order deadlock.
        var student = await _db.GetOrCreateStudentAsync(studentId, ct);

        DomainException? deferred = null;
        Guid? holdId = null;

        await using (var tx = await _db.BeginSeatTransactionAsync(ct))
        {
            var cls = await _db.LockClassAsync(classId, ct) ?? throw DomainException.NotFound("Class not found.");
            var now = await _db.GetDatabaseTimeAsync(ct);
            await _expiration.ExpireOverdueForClassAsync(classId, now, ct);

            if (!cls.AdmissionOpen)
            {
                deferred = DomainException.Conflict("ADMISSION_CLOSED", "Admission for this class is currently closed.");
            }
            else
            {
                var duplicate = await _db.Holds.AnyAsync(h =>
                    h.ClassId == classId && h.StudentId == student.Id &&
                    (h.Status == HoldStatus.Held || h.Status == HoldStatus.Confirmed), ct);

                if (duplicate)
                {
                    deferred = DomainException.Conflict("DUPLICATE_APPLICATION",
                        "You already have an active application for this class.");
                }
                else
                {
                    var counts = await _seats.GetCountsForClassAsync(classId, now, ct);
                    if (SeatMath.Available(cls.TotalSeats, counts.Held, counts.Confirmed) <= 0)
                    {
                        deferred = new SeatUnavailableException();
                    }
                    else
                    {
                        var hold = new AdmissionHold
                        {
                            Id = Guid.NewGuid(),
                            ClassId = classId,
                            StudentId = student.Id,
                            Status = HoldStatus.Held,
                            CreatedAt = now,
                            ExpiresAt = SeatMath.ComputeExpiry(now, cls.HoldDurationSeconds)
                        };
                        _db.Holds.Add(hold);
                        _audit.Log("STUDENT", studentId, "SEAT_RESERVED", "admission_hold", hold.Id,
                            new { classId, expiresAt = hold.ExpiresAt });
                        await _db.SaveChangesAsync(ct);
                        holdId = hold.Id;
                    }
                }
            }

            await tx.CommitAsync(ct);
        }

        if (deferred is not null) throw deferred;
        return (await _reader.GetDtoAsync(holdId!.Value, ct))!;
    }

    public async Task<HoldDto> GetAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        studentId = Validation.RequireStudentId(studentId);
        await _reader.RequireOwnedAsync(holdId, studentId, ct);
        return await _reader.GetDtoAsync(holdId, ct) ?? throw DomainException.NotFound("Application not found.");
    }

    /// <summary>Cancellation transaction (plan section 6.3). Students may only cancel HELD applications.</summary>
    public async Task<HoldDto> CancelAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        studentId = Validation.RequireStudentId(studentId);
        var owner = await _reader.RequireOwnedAsync(holdId, studentId, ct);

        DomainException? deferred = null;
        await using (var tx = await _db.BeginSeatTransactionAsync(ct))
        {
            await _db.LockClassAsync(owner.ClassId, ct);
            var now = await _db.GetDatabaseTimeAsync(ct);
            await _expiration.ExpireOverdueForClassAsync(owner.ClassId, now, ct);

            var hold = await _db.LockHoldAsync(holdId, ct) ?? throw DomainException.NotFound("Application not found.");
            switch (hold.Status)
            {
                case HoldStatus.Held:
                    hold.Status = HoldStatus.Cancelled;
                    hold.CancelledAt = now;
                    _audit.Log("STUDENT", studentId, "HOLD_CANCELLED", "admission_hold", hold.Id, new { hold.ClassId });
                    await _db.SaveChangesAsync(ct);
                    break;
                case HoldStatus.Cancelled:
                    break; // idempotent
                case HoldStatus.Expired:
                    deferred = new ReservationExpiredException();
                    break;
                default:
                    deferred = DomainException.Conflict("INVALID_STATE",
                        "A confirmed admission cannot be cancelled online. Please contact the school office.");
                    break;
            }
            await tx.CommitAsync(ct);
        }

        if (deferred is not null) throw deferred;
        return (await _reader.GetDtoAsync(holdId, ct))!;
    }

    public async Task<IReadOnlyList<HoldDto>> ListForStudentAsync(string studentId, CancellationToken ct)
    {
        studentId = Validation.RequireStudentId(studentId);
        var holds = await _reader.Query()
            .Where(h => h.Student!.BirthRegistrationNumber == studentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);
        var now = await _db.GetDatabaseTimeAsync(ct);
        return holds.Select(h => HoldMapper.ToDto(h, now)).ToList();
    }
}
