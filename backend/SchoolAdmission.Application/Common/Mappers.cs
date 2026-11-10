using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Domain.Entities;
using SchoolAdmission.Domain.Enums;

namespace SchoolAdmission.Application.Common;

public static class ClassMapper
{
    /// <summary>Combined display name, e.g. "Class 6 - Morning Shift".</summary>
    public static string DisplayName(string name, string shift)
    {
        if (name.Contains(shift, StringComparison.OrdinalIgnoreCase)) return name;
        var pretty = shift.Length == 0 ? shift : char.ToUpperInvariant(shift[0]) + shift[1..].ToLowerInvariant();
        return $"{name} - {pretty} Shift";
    }

    public static ClassDto ToDto(AdmissionClass c, int held, int confirmed) => new(
        c.Id, c.Name, DisplayName(c.Name, c.Shift), c.AcademicYear, c.Shift, c.TotalSeats,
        c.HoldDurationSeconds, c.AdmissionOpen, held, confirmed,
        SeatMath.Available(c.TotalSeats, held, confirmed), c.CreatedAt, c.UpdatedAt);
}

public static class HoldMapper
{
    /// <summary>Requires Class, Student and (optionally) Payment to be loaded.</summary>
    public static HoldDto ToDto(AdmissionHold h, DateTime databaseNow)
    {
        var overdue = h.Status == HoldStatus.Held && SeatMath.IsOverdue(h.ExpiresAt, databaseNow);
        var status = overdue ? HoldStatus.Expired : h.Status;
        var remaining = status == HoldStatus.Held ? SeatMath.RemainingSeconds(h.ExpiresAt, databaseNow) : 0;
        var cls = h.Class!;
        PaymentDto? payment = h.Payment is null
            ? null
            : new PaymentDto(h.Payment.PaymentStatus, h.Payment.TransactionReference, h.Payment.Amount, h.Payment.PaidAt);

        return new HoldDto(
            h.Id, h.ClassId, ClassMapper.DisplayName(cls.Name, cls.Shift), cls.AcademicYear, cls.Shift,
            h.Student!.BirthRegistrationNumber, status, h.CreatedAt, h.ExpiresAt, h.ConfirmedAt, h.CancelledAt,
            remaining, databaseNow, payment);
    }
}
