using MediatR;
using TmsApi.Application.Services;

namespace TmsApi.Application.Courses.Commands;

public class CreateCourseHandler(
    ICourseService repo,
    ICachedCourseService cachedService)
    : IRequestHandler<CreateCourseCommand, int>
{
    public async Task<int> Handle(CreateCourseCommand command, CancellationToken ct)
    {
        var course = await repo.CreateAsync(command, ct);
        await cachedService.InvalidateCourseCacheAsync(ct);
        return course.Id;
    }
}

public class DeleteCourseHandler(
    ICourseService repo,
    ICachedCourseService cachedService)
    : IRequestHandler<DeleteCourseCommand, bool>
{
    public async Task<bool> Handle(DeleteCourseCommand command, CancellationToken ct)
    {
        await repo.DeleteAsync(command.Id, ct);
        await cachedService.InvalidateCourseCacheAsync(ct);
        return true;
    }
}

public class UpdateCourseHandler(
    ICourseService repo,
    ICachedCourseService cachedService)
    : IRequestHandler<UpdateCourseCommand, bool>
{
    public async Task<bool> Handle(UpdateCourseCommand command, CancellationToken ct)
    {
        await repo.UpdateAsync(command, ct);
        await cachedService.InvalidateCourseCacheAsync(ct);
        return true;
    }
}
