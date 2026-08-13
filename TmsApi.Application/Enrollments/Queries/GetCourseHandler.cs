using MediatR;
using TmsApi.Application.DTOs;
using TmsApi.Application.Services;

namespace TmsApi.Application.Courses.Queries;

public class GetAllCoursesHandler(ICachedCourseService cachedService)
    : IRequestHandler<GetAllCoursesQuery, List<CourseDto>>
{
    public Task<List<CourseDto>> Handle(GetAllCoursesQuery request, CancellationToken ct) =>
        cachedService.GetAllCoursesAsync(ct);
}

public class GetCourseByCodeHandler(ICachedCourseService cachedService)
    : IRequestHandler<GetCourseByCodeQuery, CourseDto>
{
    public Task<CourseDto> Handle(GetCourseByCodeQuery request, CancellationToken ct) =>
        cachedService.GetCourseAsync(request.Code, ct);
}

public class SearchCoursesHandler(ICourseService repo)
    : IRequestHandler<SearchCoursesQuery, List<CourseDto>>
{
    public async Task<List<CourseDto>> Handle(SearchCoursesQuery request, CancellationToken ct)
    {
        var courses = await repo.SearchAsync(request.Term, ct);
        return courses.Select(c => new CourseDto(
            c.Id, c.Title, c.Code, c.MaxCapacity, c.Enrollments.Count)).ToList();
    }
}