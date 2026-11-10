using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Application.Common;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;
using SchoolAdmission.Domain.Enums;
using SchoolAdmission.Domain.Exceptions;

namespace SchoolAdmission.Application.Services;

/// <summary>Admin-side read models: dashboard and application search.</summary>
public class AdmissionService : IAdmissionService
{
    private readonly IAppDbContext _db;
    private readonly SeatAvailabilityService _seats;
    private readonly HoldReader _reader;

    public AdmissionService(IAppDbContext db, SeatAvailabilityService seats, HoldReader reader)
    {
        _db = db;
        _seats = seats;
        _reader = reader;
    }

    public async Task<DashboardDto> GetDashboardAsync(CancellationToken ct)
    {
        var now = await _db.GetDatabaseTimeAsync(ct);
        var classes = await _db.Classes.AsNoTracking().Select(c => new { c.Id, c.TotalSeats }).ToListAsync(ct);
        var counts = await _seats.GetCountsAsync(null, now, ct);

        int held = 0, confirmed = 0, available = 0, totalSeats = 0;
        foreach (var c in classes)
        {
            counts.TryGetValue(c.Id, out var sc);
            var h = sc?.Held ?? 0;
            var f = sc?.Confirmed ?? 0;
            held += h;
            confirmed += f;
            totalSeats += c.TotalSeats;
            available += SeatMath.Available(c.TotalSeats, h, f);
        }

        var expired = await _db.Holds.CountAsync(h =>
            h.Status == HoldStatus.Expired || (h.Status == HoldStatus.Held && h.ExpiresAt <= now), ct);
        var cancelled = await _db.Holds.CountAsync(h => h.Status == HoldStatus.Cancelled, ct);

        var recent = await _reader.Query().OrderByDescending(h => h.CreatedAt).Take(10).ToListAsync(ct);

        return new DashboardDto(classes.Count, totalSeats, available, held, confirmed, expired, cancelled,
            recent.Select(h => HoldMapper.ToDto(h, now)).ToList());
    }

    public async Task<PagedResult<HoldDto>> SearchApplicationsAsync(ApplicationQuery query, CancellationToken ct)
    {
        var now = await _db.GetDatabaseTimeAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var q = _reader.Query();
        if (query.ClassId is not null)
            q = q.Where(h => h.ClassId == query.ClassId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(h => h.Student!.BirthRegistrationNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Status) && !query.Status.Equals("ALL", StringComparison.OrdinalIgnoreCase))
        {
            var status = query.Status.Trim().ToUpperInvariant();
            if (!HoldStatus.All.Contains(status))
                throw DomainException.Validation("Unknown status filter.");

            // A HELD hold past its deadline is reported as EXPIRED even before the worker processes it.
            q = status switch
            {
                HoldStatus.Held => q.Where(h => h.Status == HoldStatus.Held && h.ExpiresAt > now),
                HoldStatus.Expired => q.Where(h => h.Status == HoldStatus.Expired ||
                                                   (h.Status == HoldStatus.Held && h.ExpiresAt <= now)),
                _ => q.Where(h => h.Status == status)
            };
        }

        var total = await q.CountAsync(ct);
        var rows = await q.OrderByDescending(h => h.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResult<HoldDto>(rows.Select(h => HoldMapper.ToDto(h, now)).ToList(), total, page, pageSize);
    }

    public async Task<HoldDto> GetApplicationAsync(Guid holdId, CancellationToken ct)
        => await _reader.GetDtoAsync(holdId, ct) ?? throw DomainException.NotFound("Application not found.");
}
