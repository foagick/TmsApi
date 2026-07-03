using Microsoft.EntityFrameworkCore;
using Tms.Api.Dtos;
using TmsApi.Data;
using TmsApi.Entities;

namespace Tms.Api.Services;
public class StudentService(TmsDbContext context, ILogger<StudentService> logger) : IStudentService
{
    public Task<StudentResponseDto?> GetByIdAsync(int id, CancellationToken ct) =>
        context.Students
            .AsNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new StudentResponseDto(s.Id, s.RegistrationNumber, s.Name, s.GPA, s.IsActive))
            .FirstOrDefaultAsync(ct);

    public async Task<StudentResponseDto> CreateAsync(CreateStudentRequest request, CancellationToken ct)
    {
        var student = new Student
        {
            RegistrationNumber = request.RegistrationNumber,
            Name = request.Name,
            GPA = request.GPA,
            IsActive = request.IsActive
        };

        context.Students.Add(student);
        await context.SaveChangesAsync(ct);

        logger.LogInformation("Created student {StudentId} ({RegistrationNumber})", student.Id, student.RegistrationNumber);

        return (await GetByIdAsync(student.Id, ct))!;
    }

    public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken ct) =>
        context.Students.AsNoTracking().AnyAsync(s => s.RegistrationNumber == registrationNumber, ct);
}
