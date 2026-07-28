using MediatR;

namespace TmsApi.Application.Courses.Commands;

public record CreateCourseCommand(string Code, string Title, int MaxCapacity) : IRequest<int>;

public record DeleteCourseCommand(int Id) : IRequest<bool>;

public record UpdateCourseCommand(int Id, string? Title, string? Code, int? MaxCapacity) : IRequest<bool>;