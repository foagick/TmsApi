using TmsApi.Entities;
namespace Tms.Api.Services;
public interface ICourseService
{
Task<Course?> GetByIdAsync(int id, CancellationToken ct);
Task<Course> CreateAsync(Course course, CancellationToken ct);
}