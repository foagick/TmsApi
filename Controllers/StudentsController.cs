using Microsoft.AspNetCore.Mvc;
using Tms.Api.Dtos;
using Tms.Api.Services;

namespace Tms.Api.Controllers;

[ApiController]
[Route("api/students")]
public class StudentsController(IStudentService studentService) : ControllerBase
{
    [HttpGet("{id:int}", Name = nameof(GetStudentById))]
    public async Task<IActionResult> GetStudentById(int id, CancellationToken ct)
    {
        var student = await studentService.GetByIdAsync(id, ct);
        return student is not null ? Ok(student) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateStudent(CreateStudentRequest request, CancellationToken ct)
    {
        if (await studentService.RegistrationNumberExistsAsync(request.RegistrationNumber, ct))
        {
            return Conflict(new ProblemDetails
            {
                Title = "Registration number already exists",
                Detail = $"A student with registration number '{request.RegistrationNumber}' already exists.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var result = await studentService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetStudentById), new { id = result.Id }, result);
    }
}
