using MediatR;
using TmsApi.Application.DTOs;

namespace TmsApi.Application.Courses.Queries;

public record GetAllCoursesQuery : IRequest<List<CourseDto>>;

public record GetCourseByCodeQuery(string Code) : IRequest<CourseDto>;

public record SearchCoursesQuery(string? Term) : IRequest<List<CourseDto>>;