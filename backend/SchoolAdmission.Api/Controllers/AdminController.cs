using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("admin")]
public class AdminController : ControllerBase
{
    private readonly IClassService _classes;
    private readonly IAdmissionService _admissions;

    public AdminController(IClassService classes, IAdmissionService admissions)
    {
        _classes = classes;
        _admissions = admissions;
    }

    private string Actor => User.Identity?.Name ?? "admin";

    [HttpGet("dashboard")]
    public async Task<ActionResult<DashboardDto>> Dashboard(CancellationToken ct)
        => Ok(await _admissions.GetDashboardAsync(ct));

    [HttpGet("classes")]
    public async Task<ActionResult<IReadOnlyList<ClassDto>>> Classes(CancellationToken ct)
        => Ok(await _classes.ListAsync(ct));

    [HttpPut("classes/{classId:guid}")]
    public async Task<ActionResult<ClassDto>> Update(Guid classId, [FromBody] UpdateClassRequest request, CancellationToken ct)
        => Ok(await _classes.UpdateDetailsAsync(classId, request, Actor, ct));

    [HttpPatch("classes/{classId:guid}/capacity")]
    public async Task<ActionResult<ClassDto>> Capacity(Guid classId, [FromBody] UpdateCapacityRequest request, CancellationToken ct)
        => Ok(await _classes.UpdateCapacityAsync(classId, request, Actor, ct));

    [HttpPatch("classes/{classId:guid}/hold-duration")]
    public async Task<ActionResult<HoldDurationResponse>> HoldDuration(Guid classId, [FromBody] UpdateHoldDurationRequest request, CancellationToken ct)
        => Ok(await _classes.UpdateHoldDurationAsync(classId, request, Actor, ct));

    [HttpPatch("classes/{classId:guid}/admission-status")]
    public async Task<ActionResult<ClassDto>> AdmissionStatus(Guid classId, [FromBody] UpdateAdmissionStatusRequest request, CancellationToken ct)
        => Ok(await _classes.UpdateAdmissionStatusAsync(classId, request, Actor, ct));

    [HttpGet("classes/{classId:guid}/applications")]
    public async Task<ActionResult<PagedResult<HoldDto>>> ClassApplications(
        Guid classId, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        await _classes.GetAsync(classId, ct); // 404 when the class does not exist
        return Ok(await _admissions.SearchApplicationsAsync(new ApplicationQuery(null, status, classId, page, pageSize), ct));
    }

    [HttpGet("applications")]
    public async Task<ActionResult<PagedResult<HoldDto>>> Applications(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] Guid? classId,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
        => Ok(await _admissions.SearchApplicationsAsync(new ApplicationQuery(search, status, classId, page, pageSize), ct));

    [HttpGet("applications/{holdId:guid}")]
    public async Task<ActionResult<HoldDto>> Application(Guid holdId, CancellationToken ct)
        => Ok(await _admissions.GetApplicationAsync(holdId, ct));
}
