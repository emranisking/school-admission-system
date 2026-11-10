using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IAppDbContext _db;
    private readonly IExpirationService _expiration;
    private readonly IAuditService _audit;
    private readonly HoldReader _reader;
    private readonly AdmissionOptions _options;

    public PaymentService(IAppDbContext db, IExpirationService expiration, IAuditService audit,
        HoldReader reader, IOptions<AdmissionOptions> options)
    {
        _db = db;
        _expiration = expiration;
        _audit = audit;
        _reader = reader;
        _options = options.Value;
    }

    /// <summary>
    /// Payment confirmation transaction (plan section 6.2). The payment is simulated, so there is no external
    /// call while locks are held. Idempotent: repeating the call returns the existing confirmation.
    /// </summary>
    public async Task<HoldDto> ConfirmAsync(Guid holdId, string studentId, CancellationToken ct)
    {
        studentId = Validation.RequireStudentId(studentId);
        var owner = await _reader.RequireOwnedAsync(holdId, studentId, ct);

        DomainException? deferred = null;
        await using (var tx = await _db.BeginSeatTransactionAsync(ct))
        {
            // 1. class lock first (consistent lock order), 2. expire overdue holds, 3. lock and re-read the hold
            await _db.LockClassAsync(owner.ClassId, ct);
            var now = await _db.GetDatabaseTimeAsync(ct);
            await _expiration.ExpireOverdueForClassAsync(owner.ClassId, now, ct);

            var hold = await _db.LockHoldAsync(holdId, ct) ?? throw DomainException.NotFound("Application not found.");
            switch (hold.Status)
            {
                case HoldStatus.Confirmed:
                    break; // idempotent: no second payment, no second seat
                case HoldStatus.Held when SeatMath.IsOverdue(hold.ExpiresAt, now):
                case HoldStatus.Expired:
                    deferred = new ReservationExpiredException();
                    break;
                case HoldStatus.Held:
                    _db.Payments.Add(new Payment
                    {
                        Id = Guid.NewGuid(),
                        HoldId = hold.Id,
                        Amount = _options.FeeAmount,
                        PaymentStatus = PaymentStatus.Success,
                        TransactionReference = "TXN-" + Convert.ToHexString(RandomNumberGenerator.GetBytes(6)),
                        PaidAt = now,
                        CreatedAt = now
                    });
                    hold.Status = HoldStatus.Confirmed;
                    hold.ConfirmedAt = now;
                    _audit.Log("STUDENT", studentId, "PAYMENT_CONFIRMED", "admission_hold", hold.Id,
                        new { hold.ClassId, amount = _options.FeeAmount });
                    await _db.SaveChangesAsync(ct);
                    break;
                default:
                    deferred = DomainException.Conflict("INVALID_STATE",
                        "This application was cancelled and can no longer be paid.");
                    break;
            }
            await tx.CommitAsync(ct);
        }

        if (deferred is not null) throw deferred;
        return (await _reader.GetDtoAsync(holdId, ct))!;
    }
}
