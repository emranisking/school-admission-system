using System.Text.RegularExpressions;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Common;

/// <summary>Pure, side-effect-free business rules (unit-testable without a database).</summary>
public static class SeatMath
{
    public static int Available(int totalSeats, int heldSeats, int confirmedSeats)
        => Math.Max(0, totalSeats - heldSeats - confirmedSeats);

    public static DateTime ComputeExpiry(DateTime databaseNow, int holdDurationSeconds)
        => databaseNow.AddSeconds(holdDurationSeconds);

    public static int RemainingSeconds(DateTime? expiresAt, DateTime databaseNow)
    {
        if (expiresAt is null) return 0;
        var seconds = (expiresAt.Value - databaseNow).TotalSeconds;
        return seconds <= 0 ? 0 : (int)Math.Floor(seconds);
    }

    public static bool IsOverdue(DateTime? expiresAt, DateTime databaseNow)
        => expiresAt is not null && expiresAt.Value <= databaseNow;
}

public static class Validation
{
    public const int MinHoldSeconds = 1;
    public const int MaxHoldSeconds = 86_400;
    public const int MaxSeats = 100_000;

    private static readonly Regex StudentIdRegex = new("^[0-9]{17}$", RegexOptions.Compiled);

    public static bool IsValidStudentId(string? value)
        => value is not null && StudentIdRegex.IsMatch(value);

    public static string RequireStudentId(string? value)
    {
        var trimmed = value?.Trim();
        if (!IsValidStudentId(trimmed))
            throw new DomainException("INVALID_STUDENT_ID",
                "The student id must be a 17-digit birth registration number.", ErrorKind.Validation);
        return trimmed!;
    }

    public static string NormalizeShift(string? shift)
    {
        var value = shift?.Trim();
        if (string.IsNullOrEmpty(value))
            throw DomainException.Validation("Shift is required.");
        if (value.Length > 50)
            throw DomainException.Validation("Shift must be 50 characters or fewer.");
        return value.ToUpperInvariant();
    }

    public static string RequireName(string? name)
    {
        var value = name?.Trim();
        if (string.IsNullOrEmpty(value))
            throw DomainException.Validation("Class name is required.");
        if (value.Length > 150)
            throw DomainException.Validation("Class name must be 150 characters or fewer.");
        return value;
    }

    public static int RequireAcademicYear(int? year)
    {
        if (year is null || year < 2000 || year > 2200)
            throw DomainException.Validation("Academic year must be between 2000 and 2200.");
        return year.Value;
    }

    public static int RequireSeats(int? seats)
    {
        if (seats is null || seats < 0 || seats > MaxSeats)
            throw DomainException.Validation($"Total seats must be between 0 and {MaxSeats}.");
        return seats.Value;
    }

    public static int RequireHoldDuration(int? seconds)
    {
        if (seconds is null || seconds < MinHoldSeconds || seconds > MaxHoldSeconds)
            throw DomainException.Validation(
                $"Hold duration must be between {MinHoldSeconds} and {MaxHoldSeconds} seconds.");
        return seconds.Value;
    }
}
