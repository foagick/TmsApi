using TmsApi.Application.Courses.Commands;
using TmsApi.Application.DTOs;
using TmsApi.Domain.Entities;

namespace TmsApi.Application.Services;

public interface ICourseService
{
    Task<CourseResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    // Task<Course?> GetByIdAsync(int id, CancellationToken ct);

    Task<CourseResponseDto> CreateAsync(CreateCourseCommand command, CancellationToken ct);
    Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct);
    // Task<Course> CreateAsync(Course course, CancellationToken ct);

    Task<bool> CodeExistsAsync(string code, CancellationToken ct);

    Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct);

    Task<Course?> GetByCodeAsync(string code, CancellationToken ct);

    Task<List<Course>> GetAllAsync(CancellationToken ct);

    Task UpdateAsync(UpdateCourseCommand command, CancellationToken ct);

    Task DeleteAsync(int id, CancellationToken ct);

    Task<List<Course>> SearchAsync(string? term, CancellationToken ct);
}