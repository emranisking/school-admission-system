using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classes;
    public ClassesController(IClassService classes) => _classes = classes;

    private string Actor => User.Identity?.Name ?? "admin";

    [AllowAnonymous]
    [HttpGet("/classes")]
    public async Task<ActionResult<IReadOnlyList<ClassDto>>> List(CancellationToken ct)
        => Ok(await _classes.ListAsync(ct));

    [AllowAnonymous]
    [HttpGet("/classes/{classId:guid}")]
    public async Task<ActionResult<ClassDto>> Get(Guid classId, CancellationToken ct)
        => Ok(await _classes.GetAsync(classId, ct));

    [Authorize(Roles = "Admin")]
    [HttpPost("/classes")]
    public async Task<ActionResult<ClassDto>> Create([FromBody] CreateClassRequest request, CancellationToken ct)
    {
        var created = await _classes.CreateAsync(request, Actor, ct);
        return Created($"/classes/{created.Id}", created);
    }
}
