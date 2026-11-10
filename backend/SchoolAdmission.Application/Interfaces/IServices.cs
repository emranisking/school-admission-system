using SchoolAdmission.Application.DTOs;

namespace SchoolAdmission.Application.Interfaces;

public interface IClassService
{
    Task<ClassDto> CreateAsync(CreateClassRequest request, string actor, CancellationToken ct);
    Task<IReadOnlyList<ClassDto>> ListAsync(CancellationToken ct);
    Task<ClassDto> GetAsync(Guid classId, CancellationToken ct);
    Task<ClassDto> UpdateDetailsAsync(Guid classId, UpdateClassRequest request, string actor, CancellationToken ct);
    Task<ClassDto> UpdateCapacityAsync(Guid classId, UpdateCapacityRequest request, string actor, CancellationToken ct);
    Task<HoldDurationResponse> UpdateHoldDurationAsync(Guid classId, UpdateHoldDurationRequest request, string actor, CancellationToken ct);
    Task<ClassDto> UpdateAdmissionStatusAsync(Guid classId, UpdateAdmissionStatusRequest request, string actor, CancellationToken ct);
}

public interface IReservationService
{
    Task<HoldDto> ReserveAsync(Guid classId, string studentId, CancellationToken ct);
    Task<HoldDto> GetAsync(Guid holdId, string studentId, CancellationToken ct);
    Task<HoldDto> CancelAsync(Guid holdId, string studentId, CancellationToken ct);
    Task<IReadOnlyList<HoldDto>> ListForStudentAsync(string studentId, CancellationToken ct);
}

public interface IPaymentService
{
    Task<HoldDto> ConfirmAsync(Guid holdId, string studentId, CancellationToken ct);
}

public interface IAdmissionService
{
    Task<DashboardDto> GetDashboardAsync(CancellationToken ct);
    Task<PagedResult<HoldDto>> SearchApplicationsAsync(ApplicationQuery query, CancellationToken ct);
    Task<HoldDto> GetApplicationAsync(Guid holdId, CancellationToken ct);
}

public interface IExpirationService
{
    /// <summary>
    /// Marks overdue HELD holds of one class as EXPIRED. MUST be called inside a transaction
    /// that already holds the class row lock.
    /// </summary>
    Task<int> ExpireOverdueForClassAsync(Guid classId, DateTime databaseNow, CancellationToken ct);

    /// <summary>Background-worker entry point: finds classes with overdue holds and expires them class by class.</summary>
    Task<int> ExpireAllOverdueAsync(CancellationToken ct);
}

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAdminToken(Guid adminId, string username);
}

public interface IAuditService
{
    /// <summary>Adds an audit row to the current unit of work. The caller saves.</summary>
    void Log(string actorType, string? actorId, string action, string entityType, Guid? entityId, object? details = null);
}
