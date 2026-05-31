using Calendar.Booking.API.Validators;
using Calendar.Contracts.Requests;
using FluentValidation.TestHelper;
using FluentAssertions;

namespace Booking.UnitTests.Validators;

[TestFixture]
public class CreateBookingRequestValidatorTests
{
    private CreateBookingRequestValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _validator = new CreateBookingRequestValidator();
    }

    [Test]
    public void CreateBookingRequest_Should_HaveError_When_TitleIsEmpty()
    {
        // Arrange
        var request = new CreateBookingRequest(string.Empty, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название бронирования обязательно.");
    }

    [Test]
    public void CreateBookingRequest_Should_HaveError_When_TitleExceedsMaxLength()
    {
        // Arrange
        var request = new CreateBookingRequest(new string('a', 201), 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Название не должно превышать 200 символов.");
    }

    [Test]
    public void CreateBookingRequest_Should_HaveError_When_RoomIdIsZeroOrNegative()
    {
        // Arrange
        var request1 = new CreateBookingRequest("Meeting", 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);
        var request2 = new CreateBookingRequest("Meeting", -1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), null);

        // Act
        var result1 = _validator.TestValidate(request1);
        var result2 = _validator.TestValidate(request2);

        // Assert
        result1.ShouldHaveValidationErrorFor(x => x.RoomId);
        result2.ShouldHaveValidationErrorFor(x => x.RoomId);
    }

    [Test]
    public void CreateBookingRequest_Should_HaveError_When_StartTimeIsEmpty()
    {
        // Arrange
        var request = new CreateBookingRequest("Meeting", 1, default, DateTimeOffset.UtcNow.AddHours(1), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartTimeUtc)
            .WithErrorMessage("Время начала обязательно.");
    }

    [Test]
    public void CreateBookingRequest_Should_HaveError_When_EndTimeIsBeforeStartTime()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(1);
        var request = new CreateBookingRequest("Meeting", 1, startTime, startTime.AddMinutes(-1), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndTimeUtc)
            .WithErrorMessage("Время окончания должно быть позже времени начала.");
    }

    [Test]
    public void CreateBookingRequest_Should_HaveError_When_DurationIsLessThan15Minutes()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(1);
        var request = new CreateBookingRequest("Meeting", 1, startTime, startTime.AddMinutes(14), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EndTimeUtc)
            .WithErrorMessage("Минимальная длительность бронирования — 15 минут.");
    }

    [Test]
    public void CreateBookingRequest_Should_NotHaveError_When_RequestIsValid()
    {
        // Arrange
        var startTime = DateTimeOffset.UtcNow.AddHours(1);
        var request = new CreateBookingRequest("Meeting", 1, startTime, startTime.AddMinutes(30), null);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
