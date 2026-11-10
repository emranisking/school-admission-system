using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Application.Interfaces;

/// <summary>
/// The single (per request scope) EF Core context used by all application services,
/// plus the PostgreSQL-specific primitives the seat-allocation logic depends on.
/// </summary>
public interface IAppDbContext
{
    DbSet<Admin> Admins { get; }
    DbSet<Student> Students { get; }
    DbSet<AdmissionClass> Classes { get; }
    DbSet<AdmissionHold> Holds { get; }
    DbSet<Payment> Payments { get; }
    DbSet<AuditLog> AuditLogs { get; }

    ChangeTracker ChangeTracker { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Authoritative database clock (clock_timestamp()). NOT now(): now() is frozen at
    /// transaction start, which can be stale after waiting on a row lock.
    /// </summary>
    Task<DateTime> GetDatabaseTimeAsync(CancellationToken ct = default);

    /// <summary>Begins a READ COMMITTED transaction with lock/statement timeouts applied.</summary>
    Task<IDbContextTransaction> BeginSeatTransactionAsync(CancellationToken ct = default);

    /// <summary>SELECT ... FOR UPDATE on the class row. Must be the first lock taken in a transaction.</summary>
    Task<AdmissionClass?> LockClassAsync(Guid classId, CancellationToken ct = default);

    /// <summary>SELECT ... FOR UPDATE on the hold row. Always taken after the class lock.</summary>
    Task<AdmissionHold?> LockHoldAsync(Guid holdId, CancellationToken ct = default);

    /// <summary>Idempotent upsert (INSERT ... ON CONFLICT DO NOTHING) executed outside any seat transaction.</summary>
    Task<Student> GetOrCreateStudentAsync(string birthRegistrationNumber, CancellationToken ct = default);
}
