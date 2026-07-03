using Microsoft.EntityFrameworkCore;
using Tms.Api.Dtos;
using TmsApi.Data;
using TmsApi.Entities;
namespace Tms.Api.Services;
public class CourseService(TmsDbContext context, ILogger<CourseService>logger) : ICourseService
{
    // public async Task<Course?> GetByIdAsync(int id, CancellationToken ct)
    //     {

    //         return await context.Courses
    //                 .AsNoTracking()
    //                 .FirstOrDefaultAsync(c => c.Id == id, ct);
    //         throw new NotImplementedException();
    //     }

    public Task<CourseResponseDto?> GetByIdAsync(
        int id, CancellationToken ct) => 
        context.Courses
        .AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
        .FirstOrDefaultAsync(ct);

    // public async Task<Course> CreateAsync(Course course, CancellationToken ct)
    //     {

    //         await context.Courses.AddAsync(course, ct);
    //             await context.SaveChangesAsync(ct);

    //             logger.LogInformation("Course created with Id {CourseId}, Code {Code}, Title {Title}", 
    //                 course.Id, course.Code, course.Title);

    //             return course;
    //         throw new NotImplementedException();
    //     }

    public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
        {
            var course = new Course
            {
                Code = request.Code,
                Title = request.Title,
                MaxCapacity = request.MaxCapacity
            };
            context.Courses.Add(course);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
            return (await GetByIdAsync(course.Id, ct))!;
        }

    public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
    context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);
    
}