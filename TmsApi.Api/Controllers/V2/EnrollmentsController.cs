using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TmsApi.Api.Hubs;
using TmsApi.Application.Enrollments.Commands;
using TmsApi.Application.Enrollments.Queries;
using TmsApi.Application.Hubs;
using TmsApi.Application.Services;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/enrollments")]
[ApiVersion("2.0")]
public class EnrollmentsController(
    IMediator mediator,
    IEnrollmentService enrollmentService,
    TmsDbContext context,
    IHubContext<TmsHub, ITmsHubClient> hubContext) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Enroll(
        EnrollStudentCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return result.Match<IActionResult>(
            onSuccess: created => CreatedAtAction(
                nameof(GetSchedule),
                new { studentId = created.StudentId },
                created),
            onFailure: error =>
            {
                var status = error.Code switch
                {
                    "course_not_found" => StatusCodes.Status404NotFound,
                    "course_full" or "already_enrolled" => StatusCodes.Status409Conflict,

                    _ => StatusCodes.Status400BadRequest
                };

                return Problem(
                    statusCode: status,
                    title: "Enrollment rejected",
                    detail: error.Message,
                    type: $"https://tms.local/errors/{error.Code}");
            });
    }

    [HttpGet("{studentId}/schedule")]
    public async Task<IActionResult> GetSchedule(
        int studentId, CancellationToken ct)
    {
        var schedule = await mediator.Send(
            new GetStudentScheduleQuery(studentId), ct);

        return Ok(schedule);
    }

    [HttpPost("{id}/approve")]
    public async Task<IActionResult> Approve(
        string id, CancellationToken ct)
    {
        if (!int.TryParse(id, out var enrollmentId))
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Detail = $"No enrollment with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });

        var enrollment = await context.Enrollments.FindAsync([enrollmentId], ct);
        if (enrollment is null)
            return NotFound(new ProblemDetails
            {
                Title = "Enrollment not found",
                Detail = $"No enrollment with id '{id}'.",
                Status = StatusCodes.Status404NotFound
            });

        enrollment.Status = "Approved";
        await context.SaveChangesAsync(ct);

        await hubContext.Clients.All
            .ReceiveEnrollmentStatusUpdated(id, "Approved");
        return NoContent();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var enrollments = await enrollmentService.GetAllAsync(ct);
        return Ok(enrollments);
    }
    
}