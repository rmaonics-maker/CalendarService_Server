using Calendar.Contracts.Requests;
using FluentValidation;

namespace Calendar.Booking.API.Validators;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequest>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Название бронирования обязательно.")
            .MaximumLength(200).WithMessage("Название не должно превышать 200 символов.");

        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("ID комнаты обязателен.")
            .GreaterThan(0).WithMessage("ID комнаты должен быть положительным числом.");

        RuleFor(x => x.StartTimeUtc)
            .NotEmpty().WithMessage("Время начала обязательно.")
            .Must(BeUtcOrUnspecified).WithMessage("Время начала должно быть в UTC.")
            .Must(BeInFuture).WithMessage("Время начала должно быть в будущем.");

        RuleFor(x => x.EndTimeUtc)
            .NotEmpty().WithMessage("Время окончания обязательно.")
            .Must(BeUtcOrUnspecified).WithMessage("Время окончания должно быть в UTC.")
            .Must(BeInFuture).WithMessage("Время окончания должно быть в будущем.")
            .GreaterThan(x => x.StartTimeUtc).WithMessage("Время окончания должно быть позже времени начала.")
            .Must((request, endTime) => (endTime - request.StartTimeUtc).TotalMinutes >= 15)
            .WithMessage("Минимальная длительность бронирования — 15 минут.");
    }

    private bool BeUtcOrUnspecified(DateTime dateTime)
    {
        return dateTime.Kind == DateTimeKind.Utc || dateTime.Kind == DateTimeKind.Unspecified;
    }

    private bool BeInFuture(DateTime dateTime)
    {
        return dateTime > DateTime.UtcNow.AddMinutes(-5); // Допускаем небольшое отклонение
    }
}
