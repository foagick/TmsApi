using Tms.Api.Dtos;
namespace Tms.Api.Services;
public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    // Task<Course?> GetByIdAsync(int id, CancellationToken ct);

    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    // Task<Course> CreateAsync(Course course, CancellationToken ct);

}