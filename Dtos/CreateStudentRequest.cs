using System.ComponentModel.DataAnnotations;

namespace Tms.Api.Dtos;
public record CreateStudentRequest
{
    [Required]
    [MaxLength(50)]
    public required string RegistrationNumber { get; init; }

    [Required]
    [MaxLength(100)]
    public required string Name { get; init; }

    [Range(0, 4, ErrorMessage = "GPA must be between 0.00 and 4.00.")]
    public decimal GPA { get; init; }

    public bool IsActive { get; init; } = true;
}
