namespace SchoolAdmission.Application.DTOs;

// ---- Classes ----
public record ClassDto(
    Guid Id,
    string Name,
    string DisplayName,
    int AcademicYear,
    string Shift,
    int TotalSeats,
    int HoldDurationSeconds,
    bool AdmissionOpen,
    int HeldSeats,
    int ConfirmedSeats,
    int AvailableSeats,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public record CreateClassRequest(
    string? Name,
    int? AcademicYear,
    string? Shift,
    int? TotalSeats,
    int? HoldDurationSeconds,
    bool? AdmissionOpen);

public record UpdateClassRequest(string? Name, int? AcademicYear, string? Shift);
public record UpdateCapacityRequest(int? TotalSeats);
public record UpdateHoldDurationRequest(int? HoldDurationSeconds);
public record UpdateAdmissionStatusRequest(bool? AdmissionOpen);
public record HoldDurationResponse(Guid ClassId, int HoldDurationSeconds, string Message);

// ---- Holds / payments ----
public record PaymentDto(string PaymentStatus, string? TransactionReference, decimal Amount, DateTime? PaidAt);

public record HoldDto(
    Guid HoldId,
    Guid ClassId,
    string ClassName,
    int AcademicYear,
    string Shift,
    string StudentId,
    string Status,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? CancelledAt,
    int RemainingSeconds,
    DateTime ServerTime,
    PaymentDto? Payment);

// ---- Admin ----
public record DashboardDto(
    int TotalClasses,
    int TotalSeats,
    int AvailableSeats,
    int HeldSeats,
    int ConfirmedAdmissions,
    int ExpiredReservations,
    int CancelledApplications,
    IReadOnlyList<HoldDto> RecentApplications);

public record ApplicationQuery(string? Search, string? Status, Guid? ClassId, int Page = 1, int PageSize = 20);

public record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public record LoginRequest(string? Username, string? Password);
public record LoginResponse(string Token, DateTime ExpiresAt, string Username);
