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
        var validationErrors = ValidateUpdate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new ValidationProblemDetails(validationErrors));

        await mediator.Send(new UpdateCourseCommand(id, request.Title, request.Code, request.MaxCapacity), ct);
        return NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateCourse(
        CreateCourseRequest request,
        CancellationToken ct)
    {
        var validationErrors = ValidateCreate(request);
        if (validationErrors.Count > 0)
            return BadRequest(new ValidationProblemDetails(validationErrors));

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

    private static Dictionary<string, string[]> ValidateCreate(CreateCourseRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.Code))
            errors[nameof(request.Code)] = ["Code is required."];
        else if (request.Code.Length > 50)
            errors[nameof(request.Code)] = ["Code must be 50 characters or fewer."];
        if (string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["Title is required."];
        else if (request.Title.Length > 200)
            errors[nameof(request.Title)] = ["Title must be 200 characters or fewer."];
        if (request.MaxCapacity <= 0)
            errors[nameof(request.MaxCapacity)] = ["Max capacity must be greater than zero."];
        return errors;
    }

    private static Dictionary<string, string[]> ValidateUpdate(UpdateCourseRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request.Title is null && request.Code is null && request.MaxCapacity is null)
            errors["request"] = ["At least one course field must be provided."];
        if (request.Title is not null && request.Title.Length > 200)
            errors[nameof(request.Title)] = ["Title must be 200 characters or fewer."];
        if (request.Code is not null && request.Code.Length > 50)
            errors[nameof(request.Code)] = ["Code must be 50 characters or fewer."];
        if (request.MaxCapacity is <= 0)
            errors[nameof(request.MaxCapacity)] = ["Max capacity must be greater than zero."];
        return errors;
    }
}
