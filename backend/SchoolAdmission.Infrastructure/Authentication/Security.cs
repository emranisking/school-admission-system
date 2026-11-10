using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Infrastructure.Authentication;

public class JwtOptions
{
    public string Key { get; set; } = "";
    public string Issuer { get; set; } = "school-admission";
    public string Audience { get; set; } = "school-admission-admin";
    public int ExpiryMinutes { get; set; } = 480;
}

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);

    public bool Verify(string password, string hash)
    {
        try { return BCrypt.Net.BCrypt.Verify(password, hash); }
        catch (Exception) { return false; }
    }
}

public class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    public JwtTokenService(IOptions<JwtOptions> options) => _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateAdminToken(Guid adminId, string username)
    {
        var expires = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));
        var claims = new[]
        {
            new Claim("sub", adminId.ToString()),
            new Claim("name", username),
            new Claim("role", "Admin"),
            new Claim("jti", Guid.NewGuid().ToString())
        };
        var jwt = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            notBefore: DateTime.UtcNow, expires: expires,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}
