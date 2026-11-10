namespace SchoolAdmission.Domain.Enums;

/// <summary>Hold statuses are stored as VARCHAR in PostgreSQL, so they are string constants.</summary>
public static class HoldStatus
{
    public const string Held = "HELD";
    public const string Confirmed = "CONFIRMED";
    public const string Expired = "EXPIRED";
    public const string Cancelled = "CANCELLED";

    public static readonly string[] All = { Held, Confirmed, Expired, Cancelled };
}

public static class PaymentStatus
{
    public const string Pending = "PENDING";
    public const string Success = "SUCCESS";
    public const string Failed = "FAILED";
}
