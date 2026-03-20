using AutoFixture;
using Calendar_Service.Validators;
using Calendar.Contracts;
using Calendar.Contracts.Events;
using FluentValidation.TestHelper;

namespace Calendar.UnitTests.Validators;

public class CreateEventRequestValidatorTest
{
    private readonly CreateEventRequestValidator _validator = new();
    private readonly IFixture _fixture = new Fixture();

#region Title 
    
    [Fact]
    public void Title_Should_HaveError_When_Empty()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.Title, "")
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title is required.");
    }

    [Fact]
    public void Title_Should_HaveError_When_ExceedsMaxLength()
    {
        // Arrange
        var longTitle = new string('A', 101);
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.Title, longTitle)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title)
            .WithErrorMessage("Title cannot be longer than 100 characters.");
    }

    [Fact]
    public void Title_Should_NotHaveError_When_Valid()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .Create();
        
        // Act
        var result = _validator.TestValidate(request);
        
        //Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Title);
    }
    
#endregion

#region Description

    [Fact]
    public void Description_Should_HaveError_When_ExceedsMaxLength()
    {
        // Arrange
        var longDescription = new string('A', 501);
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.Description, longDescription)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Description)
            .WithErrorMessage("Description cannot be longer than 500 characters.");
    }

    [Fact]
    public void Description_Should_NotHaveError_When_Valid()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Description);
    }

#endregion

#region Duration

    [Fact]
    public void Duration_Should_HaveError_When_Zero()
    {
        //Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.Duration, TimeSpan.Zero)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration)
            .WithErrorMessage("Duration is required.");

    }

    [Fact]
    public void Duration_Should_HaveError_When_Negative()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.Duration, TimeSpan.FromHours(-2))
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Duration)
            .WithErrorMessage("Duration must be positive.");
    }

    [Fact]
    public void Duration_Should_NotHaveError_When_Valid()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Duration);
    }

#endregion

#region StartTime

    [Fact]
    public void StartTime_Should_HaveError_When_Empty()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.StartTime, DateTimeOffset.MinValue)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.StartTime)
            .WithErrorMessage("Start time is required.");
    }

    [Fact]
    public void StartTime_Should_HaveError_When_InPast()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.StartTime, DateTimeOffset.UtcNow.AddDays(-1))
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        //Assert
        result.ShouldHaveValidationErrorFor(x => x.StartTime)
            .WithErrorMessage("Start time must be in the future.");
    }

    [Fact]
    public void StartTime_Should_HaveNotError_When_InFuture()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.StartTime, DateTimeOffset.UtcNow.AddDays(2))
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.StartTime);
    }

#endregion
    
#region UserID

    [Fact]
    public void UserId_Should_HaveError_When_Empty()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x => x.UserId, Guid.Empty)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("UserId is required and cannot be empty.");
    }

    [Fact]
    public void UserId_Should_NotHaveError_When_Valid()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }
#endregion

#region Complex
    [Fact]
    public void Should_HaveMultipleErrors_When_MultipleFieldsInvalid()
    {
        // Arrange
        var request = new CreateEventRequest(
            Title: "",
            Description: new string('A', 501),
            StartTime: DateTimeOffset.UtcNow.AddDays(-1),
            Duration: TimeSpan.Zero,
            Location: "Test location",
            RecurrentRule: null,
            UserId: Guid.Empty);
        
        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Title);
        result.ShouldHaveValidationErrorFor(x => x.Description);
        result.ShouldHaveValidationErrorFor(x => x.StartTime);
        result.ShouldHaveValidationErrorFor(x => x.Duration);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }

    [Fact]
    public void Should_NotHaveErrors_When_AllFieldsValid()
    {
        // Arrange
        var request = _fixture.Build<CreateEventRequest>()
            .With(x=>x.Title, "Test title")
            .With(x=>x.Description, "Test discription")
            .With(x=>x.Duration, TimeSpan.FromHours(6))
            .With(x=>x.StartTime, DateTimeOffset.UtcNow.AddDays(2))
            .Create();
        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
#endregion
}