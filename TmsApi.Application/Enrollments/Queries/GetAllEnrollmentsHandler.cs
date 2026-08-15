using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Services;

namespace TmsApi.Application.Enrollments.Queries;

public class GetAllEnrollmentsHandler(IEnrollmentService enrollmentService)
    : IRequestHandler<GetAllEnrollmentsQuery, IReadOnlyList<EnrollmentDetailsDto>>
{
    public Task<IReadOnlyList<EnrollmentDetailsDto>> Handle(GetAllEnrollmentsQuery request, CancellationToken ct)
    {
        return enrollmentService.GetAllDetailsAsync(ct);
    }
}
