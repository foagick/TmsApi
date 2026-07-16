using TmsApi.Application.DTOs;

namespace TmsApi.Infrastructure.Persistence;
public interface IStudentService
{
    Task<StudentResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<StudentResponseDto> CreateAsync(CreateStudentRequest request, CancellationToken ct);
    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken ct);
}
