using Microsoft.EntityFrameworkCore;
using TmsApi.Data;
using TmsApi.Entities;
namespace Tms.Api.Services;
public class CourseService(TmsDbContext context, ILogger<CourseService>logger) : ICourseService
{
public async Task<Course?> GetByIdAsync(int id, CancellationToken ct)
    {
        // TODO 1: Use context.Courses.AsNoTracking()
        //and return FirstOrDefaultAsync(c => c.Id == id, ct).
        return await context.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);
          throw new NotImplementedException();
    }

public async Task<Course> CreateAsync(Course course, CancellationToken ct)
    {
        //TODO 2: Add course to context.Courses,
        // SaveChangesAsync(ct), log info, and return the created course.
        await context.Courses.AddAsync(course, ct);
            await context.SaveChangesAsync(ct);

            logger.LogInformation("Course created with Id {CourseId}, Code {Code}, Title {Title}", 
                course.Id, course.Code, course.Title);

            return course;
        throw new NotImplementedException();
    }
}