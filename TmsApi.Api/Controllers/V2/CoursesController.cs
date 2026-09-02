using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.Courses.Queries;
using TmsApi.Application.DTOs;
using TmsApi.Application.Services;

namespace TmsApi.Api.Controllers.V2;

[ApiController]
[Route("api/v{version:apiVersion}/courses")]
[ApiVersion("2.0")]
public class CoursesController(IMediator mediator, ICourseService courseService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetCourses(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var courses = await courseService.GetCoursesAsync(new PagedRequest
        {
            Page = page,
            PageSize = pageSize,
            Search = search
        }, ct);
        return Ok(courses);
    }

    [HttpGet("search")]
    [EnableRateLimiting("search")]
    public async Task<IActionResult> SearchCourses(
        [FromQuery] string? term,
        CancellationToken ct)
    {
        var results = await mediator.Send(new SearchCoursesQuery(term), ct);
        return Ok(results);
    }

    [HttpGet("{code}")]
    public async Task<IActionResult> GetCourseByCode(string code, CancellationToken ct)
    {
        var course = await mediator.Send(new GetCourseByCodeQuery(code), ct);
        return Ok(course);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateCourse(
        int id,
        UpdateCourseRequest request,
        CancellationToken ct)
    {
        await mediator.Send(new UpdateCourseCommand(id, request.Title, request.Code, request.MaxCapacity), ct);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        CreateCourseRequest request,
        CancellationToken ct)
    {
        var id = await mediator.Send(
            new CreateCourseCommand(request.Code, request.Title, request.MaxCapacity),
            ct);
        return CreatedAtAction(nameof(GetCourseByCode), new { code = request.Code }, new { id });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteCourse(int id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCourseCommand(id), ct);
        return NoContent();
    }
}
