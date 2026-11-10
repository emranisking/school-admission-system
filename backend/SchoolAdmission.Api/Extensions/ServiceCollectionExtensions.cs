using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Application.Services;
using SchoolAdmission.Infrastructure.Authentication;
using SchoolAdmission.Infrastructure.BackgroundServices;
using SchoolAdmission.Infrastructure.Persistence;

namespace SchoolAdmission.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchoolAdmission(this IServiceCollection services, IConfiguration config)
    {
        // ---- options ----
        services.Configure<AdmissionOptions>(o =>
        {
            config.GetSection("Admission").Bind(o);
            // HOLD_DURATION_SECONDS is only the default for classes created without an explicit duration.
            if (int.TryParse(config["HOLD_DURATION_SECONDS"], out var seconds) && seconds > 0)
                o.DefaultHoldDurationSeconds = seconds;
        });
        services.Configure<JwtOptions>(config.GetSection("Jwt"));

        // ---- persistence ----
        var connectionString = config.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is not configured.");
        services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connectionString).UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // ---- application services (modules talk through these, never over HTTP) ----
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<SeatAvailabilityService>();
        services.AddScoped<HoldReader>();
        services.AddScoped<IExpirationService, ExpirationService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IAdmissionService, AdmissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        // ---- background expiration ----
        if (config.GetValue("Worker:Enabled", true))
        {
            var interval = TimeSpan.FromSeconds(Math.Max(1, config.GetValue("Worker:IntervalSeconds", 5)));
            services.AddHostedService(sp => new ReservationExpirationWorker(
                sp.GetRequiredService<IServiceScopeFactory>(),
                sp.GetRequiredService<ILogger<ReservationExpirationWorker>>(),
                interval));
        }

        return services;
    }

    public static IServiceCollection AddAdminAuthentication(this IServiceCollection services, IConfiguration config)
    {
        var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.Key) || Encoding.UTF8.GetByteCount(jwt.Key) < 32)
            throw new InvalidOperationException("Jwt:Key must be configured and at least 32 bytes long.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = "name",
                    RoleClaimType = "role"
                };
            });
        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddApiConventions(this IServiceCollection services)
    {
        services.AddControllers().ConfigureApiBehaviorOptions(o =>
        {
            o.InvalidModelStateResponseFactory = ctx =>
            {
                var message = string.Join(" ", ctx.ModelState.Values
                    .SelectMany(v => v.Errors).Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Invalid request body." : e.ErrorMessage));
                return new BadRequestObjectResult(new { error = new { code = "VALIDATION_ERROR", message } });
            };
        });

        services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
        return services;
    }
}
