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

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.EnrolledAt)
            .Select(e => new EnrollmentResponseDto(e.Id, e.CourseId, e.StudentId, e.EnrolledAt))
            .ToListAsync(ct);
    }

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId,EnrollStudentRequest request, CancellationToken ct)
    {
        

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