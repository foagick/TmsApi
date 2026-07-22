using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Common;
using TmsApi.Application.Courses.Commands;
using TmsApi.Application.DTOs;
using TmsApi.Application.Services;
using TmsApi.Domain.Entities;
using TmsApi.Infrastructure.Persistence;

namespace TmsApi.Infrastructure.Services;
public class CourseService(TmsDbContext context, ILogger<CourseService>logger) : ICourseService
{
    // public async Task<Course?> GetByIdAsync(int id, CancellationToken ct)
    //     {

    //         return await context.Courses
    //                 .AsNoTracking()
    //                 .FirstOrDefaultAsync(c => c.Id == id, ct);
    //         throw new NotImplementedException();
    //     }

        public Task<CourseResponseDto?> GetByIdAsync(
            int id, CancellationToken ct) => 
        context.Courses
        .AsNoTracking()
        .Where(c => c.Id == id)
        .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
        .FirstOrDefaultAsync(ct);

    // public async Task<Course> CreateAsync(Course course, CancellationToken ct)
    //     {

    //         await context.Courses.AddAsync(course, ct);
    //             await context.SaveChangesAsync(ct);

    //             logger.LogInformation("Course created with Id {CourseId}, Code {Code}, Title {Title}", 
    //                 course.Id, course.Code, course.Title);

    //             return course;
    //         throw new NotImplementedException();
    //     }

        public async Task<CourseResponseDto> CreateAsync(CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
                Code = request.Code,
                Title = request.Title,
                MaxCapacity = request.MaxCapacity
        };

            context.Courses.Add(course);
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
            return (await GetByIdAsync(course.Id, ct))!;
        }

        public async Task<CourseResponseDto> CreateAsync(CreateCourseCommand command, CancellationToken ct)
    {
        var course = new Course
        {
            Code = command.Code,
            Title = command.Title,
            MaxCapacity = command.MaxCapacity
        };
        context.Courses.Add(course);
        await context.SaveChangesAsync(ct);
        logger.LogInformation("Created course {CourseId} ({Code})", course.Id, course.Code);
        return (await GetByIdAsync(course.Id, ct))!;
    }

        public async Task<PagedResponse<CourseResponseDto>> GetCoursesAsync(PagedRequest request, CancellationToken ct)
        {
            IQueryable<Course> query = context.Courses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var searchPattern = $"%{request.Search}%";
                query = query.Where(c =>
                    EF.Functions.ILike(c.Title, searchPattern) ||
                    EF.Functions.ILike(c.Code, searchPattern));
            }

            var totalCount = await query.CountAsync(ct);

            var sortedQuery = request.OrderBy switch
            {
                "Code" => request.Descending ? query.OrderByDescending(c => c.Code) : query.OrderBy(c => c.Code),
                "MaxCapacity" => request.Descending ? query.OrderByDescending(c => c.MaxCapacity) : query.OrderBy(c => c.MaxCapacity),
                _ => request.Descending ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            };

            var items = await sortedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new CourseResponseDto(c.Id, c.Code, c.Title, c.MaxCapacity, c.Enrollments.Count))
                .ToListAsync(ct);

            return new PagedResponse<CourseResponseDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public Task<bool> CodeExistsAsync(string code, CancellationToken ct) =>
        context.Courses.AsNoTracking().AnyAsync(c => c.Code == code, ct);

        public Task<Course?> GetByCodeAsync(string code, CancellationToken ct) =>
            context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Code == code, ct);

        public Task<List<Course>> GetAllAsync(CancellationToken ct)
        {
            return context.Courses
                .AsNoTracking()
                .Include(c => c.Enrollments)
                .OrderBy(c => c.Title)
                .ToListAsync(ct);
        }
        
        public async Task UpdateAsync(UpdateCourseCommand command, CancellationToken ct)
        {
            var course = await context.Courses.FindAsync([command.Id], ct)
                ?? throw new NotFoundException($"Course {command.Id} not found.");

            if (command.Title is not null)
                course.Title = command.Title;

            if (command.Code is not null)
                course.Code = command.Code;

            if (command.MaxCapacity is not null)
                course.MaxCapacity = command.MaxCapacity.Value;

            await context.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(int id, CancellationToken ct)
        {
            var course = await context.Courses.FindAsync([id], ct)
                ?? throw new NotFoundException($"Course {id} not found.");

            context.Courses.Remove(course);
            await context.SaveChangesAsync(ct);
        }

        public Task<List<Course>> SearchAsync(string? term, CancellationToken ct)
    {
        IQueryable<Course> query = context.Courses.AsNoTracking().Include(c => c.Enrollments);

        if (!string.IsNullOrWhiteSpace(term))
        {
            var searchPattern = $"%{term}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Title, searchPattern) ||
                EF.Functions.ILike(c.Code, searchPattern));
        }

        return query.OrderBy(c => c.Title).ToListAsync(ct);
    }
}