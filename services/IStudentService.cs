using Tms.Api.Dtos;

namespace Tms.Api.Services;
public interface IStudentService
{
    Task<StudentResponseDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<StudentResponseDto> CreateAsync(CreateStudentRequest request, CancellationToken ct);
    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken ct);
}
