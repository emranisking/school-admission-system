using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

public class AuthService : IAuthService
{
    // Verified when the username is unknown so response time does not reveal which usernames exist.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing");

    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokens;
    private readonly IAuditService _audit;

    public AuthService(IAppDbContext db, IPasswordHasher hasher, ITokenService tokens, IAuditService audit)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
        _audit = audit;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var username = request.Username?.Trim();
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(request.Password))
            throw DomainException.Validation("Username and password are required.");

        var admin = await _db.Admins.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username, ct);
        var ok = _hasher.Verify(request.Password, admin?.PasswordHash ?? DummyHash);
        if (admin is null || !ok)
            throw new DomainException("INVALID_CREDENTIALS", "Invalid username or password.", ErrorKind.Unauthorized);

        var (token, expiresAt) = _tokens.CreateAdminToken(admin.Id, admin.Username);
        _audit.Log("ADMIN", admin.Username, "ADMIN_LOGIN", "admin", admin.Id);
        await _db.SaveChangesAsync(ct);
        return new LoginResponse(token, expiresAt, admin.Username);
    }
}
