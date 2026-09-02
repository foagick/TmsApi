using FluentValidation;

namespace TmsApi.Application.Courses.Commands;

public sealed class CreateCourseCommandValidator : AbstractValidator<CreateCourseCommand>
{
    public CreateCourseCommandValidator()
    {
        RuleFor(command => command.Code)
            .NotEmpty()
            .MaximumLength(50);
        RuleFor(command => command.Title)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(command => command.MaxCapacity)
            .GreaterThan(0);
    }
}

public sealed class UpdateCourseCommandValidator : AbstractValidator<UpdateCourseCommand>
{
    public UpdateCourseCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0);
        RuleFor(command => command.Title)
            .MaximumLength(200)
            .When(command => command.Title is not null);
        RuleFor(command => command.Code)
            .MaximumLength(50)
            .When(command => command.Code is not null);
        RuleFor(command => command.MaxCapacity)
            .GreaterThan(0)
            .When(command => command.MaxCapacity is not null);
        RuleFor(command => command)
            .Must(command => command.Title is not null || command.Code is not null || command.MaxCapacity is not null)
            .WithMessage("At least one course field must be provided.");
    }
}