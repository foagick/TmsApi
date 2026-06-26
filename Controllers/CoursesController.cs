using Microsoft.AspNetCore.Mvc;
using Tms.Api.Services;
using TmsApi.Entities;

namespace Tms.Api.Controllers
{
    [ApiController]
    [Route("api/courses")]
    public class CoursesController(ICourseService courseService) : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id, CancellationToken ct)
        {
            var course = await courseService.GetByIdAsync(id, ct);
            return course is not null ? Ok(course) : NotFound();
            throw new NotImplementedException();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCourse(Course course, CancellationToken ct)
        // public async Task<IActionResult> Create([FromBody] Course course, CancellationToken ct)
        {
            var created = await courseService.CreateAsync(course, ct);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            throw new NotImplementedException();
        }
    }
}
