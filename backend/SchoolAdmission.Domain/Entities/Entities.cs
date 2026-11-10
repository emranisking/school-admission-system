namespace SchoolAdmission.Domain.Entities;

public class Admin
{
    public Guid Id { get; set; }
    public string Username { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class Student
{
    public Guid Id { get; set; }
    public string BirthRegistrationNumber { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}

public class AdmissionClass
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public int AcademicYear { get; set; }
    public string Shift { get; set; } = "";
    public int TotalSeats { get; set; }
    public int HoldDurationSeconds { get; set; } = 600;
    public bool AdmissionOpen { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class AdmissionHold
{
    public Guid Id { get; set; }
    public Guid ClassId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = Enums.HoldStatus.Held;
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public AdmissionClass? Class { get; set; }
    public Student? Student { get; set; }
    public Payment? Payment { get; set; }
}

public class Payment
{
    public Guid Id { get; set; }
    public Guid HoldId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = Enums.PaymentStatus.Pending;
    public string? TransactionReference { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public AdmissionHold? Hold { get; set; }
}

public class AuditLog
{
    public Guid Id { get; set; }
    public string ActorType { get; set; } = "";
    public string? ActorId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid? EntityId { get; set; }
    /// <summary>JSON document stored in a PostgreSQL jsonb column.</summary>
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}
