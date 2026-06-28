using Calendar.Contracts.Requests;
using FluentValidation;

namespace Calendar_Service.Validators;

public class CreateAppointmentRequestValidator : AbstractValidator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title cannot be longer than 100 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot be longer than 500 characters.");
        
        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Duration is required.")
            .GreaterThan(TimeSpan.Zero).WithMessage("Duration must be positive.");
        
        RuleFor(x => x.StartTime)
            .NotEmpty().WithMessage("Start time is required.")
            .GreaterThan(DateTimeOffset.UtcNow).WithMessage("Start time must be in the future.");

        RuleFor(x => x.RoomId)
            .GreaterThan(0).WithMessage("RoomId must be a positive number.")
            .When(x => x.RoomId.HasValue);
    }
}