using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.DTOs;
using TmsApi.Application.Services;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;

public class EnrollmentService(TmsDbContext context, ILogger<EnrollmentService> logger) : IEnrollmentService
{
    public Task<EnrollmentResponseDto?> GetByIdAsync(int courseId, int id, CancellationToken ct) =>
        context.Enrollments
            .AsNoTracking()
            .Where(e => e.Id == id && e.CourseId == courseId)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetByCourseAsync(int courseId, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.CourseId == courseId)
            .OrderBy(e => e.EnrolledAt)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<EnrollmentResponseDto>> GetAllAsync(CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .OrderByDescending(e => e.EnrolledAt)
            .Select(e => new EnrollmentResponseDto(
                e.Id,
                e.CourseId,
                e.StudentId,
                e.EnrolledAt))
            .ToListAsync(ct);
    }

    

    public async Task<EnrollmentResponseDto> CreateAsync(int courseId, EnrollStudentRequest request,
        CancellationToken ct)
    {
        // Validate that the student exists
        var studentExists = await context.Students
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.StudentId, ct);

        if (!studentExists)
        {
            throw new KeyNotFoundException($"Student with ID {request.StudentId} not found.");
        }

        // Validate that the course exists
        var courseExists = await context.Courses
            .AsNoTracking()
            .AnyAsync(c => c.Id == courseId, ct);

        if (!courseExists)
        {
            throw new KeyNotFoundException($"Course with ID {courseId} not found.");
        }

        // Check if student is already enrolled in the course
        var alreadyEnrolled = await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == request.StudentId && e.CourseId == courseId, ct);

        if (alreadyEnrolled)
        {
            throw new InvalidOperationException(
                $"Student {request.StudentId} is already enrolled in course {courseId}.");
        }

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

    public async Task<bool> ExistsAsync(int studentId, string courseCode, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .AnyAsync(e => e.StudentId == studentId && e.Course.Code == courseCode, ct);
    }

    public async Task AddAsync(Enrollment enrollment, CancellationToken ct)
    {
        context.Enrollments.Add(enrollment);
        await context.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<Enrollment>> GetByStudentIdAsync(int studentId, CancellationToken ct)
    {
        return await context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId)
            .Include(e => e.Course)
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(ct);
    }
}