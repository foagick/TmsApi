namespace TmsApi.Application.DTOs;

public record EnrollmentDetailsDto(
    string Id,
    int StudentId,
    string StudentName,
    int CourseId,
    string CourseName,
    string Status,
    DateTime EnrolledAt
);
