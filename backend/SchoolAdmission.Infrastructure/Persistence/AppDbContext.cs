using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Entities;

namespace SchoolAdmission.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Admin> Admins => Set<Admin>();
    public DbSet<Student> Students => Set<Student>();
    public DbSet<AdmissionClass> Classes => Set<AdmissionClass>();
    public DbSet<AdmissionHold> Holds => Set<AdmissionHold>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public async Task<DateTime> GetDatabaseTimeAsync(CancellationToken ct = default)
    {
        return await Database.SqlQueryRaw<DateTime>("SELECT clock_timestamp() AS \"Value\"").SingleAsync(ct);
    }

    public async Task<IDbContextTransaction> BeginSeatTransactionAsync(CancellationToken ct = default)
    {
        var tx = await Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        await Database.ExecuteSqlRawAsync(
            "SET LOCAL lock_timeout = '10s'; SET LOCAL statement_timeout = '30s'", Array.Empty<object>(), ct);
        return tx;
    }

    public async Task<AdmissionClass?> LockClassAsync(Guid classId, CancellationToken ct = default)
    {
        // ToListAsync (not FirstOrDefault) so EF does not wrap the FOR UPDATE query in a sub-select.
        var rows = await Classes
            .FromSqlInterpolated($"SELECT * FROM classes WHERE id = {classId} FOR UPDATE")
            .ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public async Task<AdmissionHold?> LockHoldAsync(Guid holdId, CancellationToken ct = default)
    {
        var rows = await Holds
            .FromSqlInterpolated($"SELECT * FROM admission_holds WHERE id = {holdId} FOR UPDATE")
            .ToListAsync(ct);
        return rows.FirstOrDefault();
    }

    public async Task<Student> GetOrCreateStudentAsync(string birthRegistrationNumber, CancellationToken ct = default)
    {
        await Database.ExecuteSqlInterpolatedAsync(
            $@"INSERT INTO students (id, birth_registration_number, created_at)
               VALUES ({Guid.NewGuid()}, {birthRegistrationNumber}, clock_timestamp())
               ON CONFLICT (birth_registration_number) DO NOTHING", ct);

        return await Students.AsNoTracking()
            .FirstAsync(s => s.BirthRegistrationNumber == birthRegistrationNumber, ct);
    }
}
