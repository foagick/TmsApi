using Microsoft.EntityFrameworkCore;
using Tms.Api.Dtos;
using Tms.Api.Services;
using TmsApi.Data;
using TmsApi.Entities;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
    context.Enrollments
    .AsNoTracking()
    .Where(e => e.Id == id && e.CourseId == courseId)
    .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
    .FirstOrDefaultAsync(ct);

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId,EnrollStudentRequest request, CancellationToken ct)
    {
        // TODO 2: Insert a new Enrollment with CourseId = courseId, StudentId = request.StudentId,
        // and EnrolledAt = DateTime.UtcNow. SaveChangesAsync(ct), log info, then re-read
        // through GetByIdAsync(courseId, enrollment.Id, ct).
        var enrollment = new Enrollment
        {
            CourseId = courseId,
            StudentId = request.StudentId,
            EnrolledAt = DateTime.UtcNow
        };
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created enrollment {EnrollmentId} for student {StudentId} in course {CourseId}",
            enrollment.Id, enrollment.StudentId, enrollment.CourseId);

        return await GetByIdAsync(courseId, enrollment.Id, ct)
            ?? throw new InvalidOperationException("Enrollment was not found after creation.");

    }
}

// public interface IEnrollmentService
// {
// Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode);
// Task<EnrollmentRecord?> GetByIdAsync(string id);
// Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync();
// Task<bool> DeleteAsync(string id);
// }
// //--- The in-memory implementation--
// public class EnrollmentService : IEnrollmentService
// {
// private readonly Dictionary<string, EnrollmentRecord> _store = new();
// private readonly ILogger<EnrollmentService> _logger;
// public EnrollmentService(ILogger<EnrollmentService> logger)
// {
// _logger = logger;
// }
// public Task<EnrollmentRecord> EnrollAsync(string studentId, string courseCode)
// {
// var id = Guid.NewGuid().ToString("N")[..8];
// var record = new EnrollmentRecord(id, studentId, courseCode, DateTime.UtcNow);
// _store[id] = record;
// _logger.LogInformation(
// "Enrolled {StudentId} in {CourseCode} record {EnrollmentId}",
// studentId, courseCode, id);
// return Task.FromResult(record);
// }
// public Task<EnrollmentRecord?> GetByIdAsync(string id)
// {
// _store.TryGetValue(id, out var record);
// return Task.FromResult(record);
// }
// public Task<IReadOnlyList<EnrollmentRecord>> GetAllAsync()
// {
// IReadOnlyList<EnrollmentRecord> all = _store.Values.ToList();
// return Task.FromResult(all);
// }
// public Task<bool> DeleteAsync(string id)
// {
// var removed = _store.Remove(id);
// return Task.FromResult(removed);
// }
// }
// //--- The data shape--
// public record EnrollmentRecord(
// string Id,
// string StudentId,
// string CourseCode,
// DateTime EnrolledAt);