using Calendar.Contracts.Requests;
using FluentValidation;

namespace Calendar_Service.Validators;

public class UpdateAppointmentRequestValidator : AbstractValidator<UpdateAppointmentRequest>
{
    public UpdateAppointmentRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(100).WithMessage("Title cannot be longer than 100 characters.");
        
        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description cannot be longer than 500 characters.");
        
        RuleFor(x => x.Duration)
            .NotEmpty().WithMessage("Duration is required.")
            .GreaterThan(TimeSpan.Zero).WithMessage("Duration must be positive.");
    }
}