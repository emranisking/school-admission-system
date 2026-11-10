using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolAdmission.Application.DTOs;
using SchoolAdmission.Application.Interfaces;

namespace SchoolAdmission.Api.Controllers;

[ApiController]
[AllowAnonymous] // students are identified by the X-Student-Id header contract
public class HoldsController : ControllerBase
{
    private readonly IReservationService _reservations;
    private readonly IPaymentService _payments;

    public HoldsController(IReservationService reservations, IPaymentService payments)
    {
        _reservations = reservations;
        _payments = payments;
    }

    [HttpPost("/classes/{classId:guid}/holds")]
    public async Task<ActionResult<HoldDto>> Reserve(Guid classId, CancellationToken ct)
    {
        var hold = await _reservations.ReserveAsync(classId, StudentIdentity.Require(Request), ct);
        return Created($"/holds/{hold.HoldId}", hold);
    }

    [HttpGet("/holds/{holdId:guid}")]
    public async Task<ActionResult<HoldDto>> Get(Guid holdId, CancellationToken ct)
        => Ok(await _reservations.GetAsync(holdId, StudentIdentity.Require(Request), ct));

    [HttpPost("/holds/{holdId:guid}/confirm")]
    public async Task<ActionResult<HoldDto>> Confirm(Guid holdId, CancellationToken ct)
        => Ok(await _payments.ConfirmAsync(holdId, StudentIdentity.Require(Request), ct));

    [HttpDelete("/holds/{holdId:guid}")]
    public async Task<ActionResult<HoldDto>> Cancel(Guid holdId, CancellationToken ct)
        => Ok(await _reservations.CancelAsync(holdId, StudentIdentity.Require(Request), ct));

    [HttpGet("/me/holds")]
    public async Task<ActionResult<IReadOnlyList<HoldDto>>> MyHolds(CancellationToken ct)
        => Ok(await _reservations.ListForStudentAsync(StudentIdentity.Require(Request), ct));
}
