using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TmsApi.Application.Behaviors;
using TmsApi.Application.Enrollments.Commands;

namespace TmsApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(EnrollStudentHandler).Assembly));

        services.AddValidatorsFromAssembly(typeof(EnrollStudentValidator).Assembly);

        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
