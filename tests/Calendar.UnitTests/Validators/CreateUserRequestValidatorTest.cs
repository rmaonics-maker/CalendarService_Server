using AutoFixture;
using Calendar_Service.Validators;
using Calendar.Contracts;
using FluentValidation.TestHelper;

namespace Calendar.UnitTests.Validators;

public class CreateUserRequestValidatorTest
{
    private readonly CreateUserRequestValidator _validator = new();
    private readonly IFixture _fixture = new Fixture();

    [Fact]
    public void Name_Should_HaveError_When_Empty()
    {
        // Arrange
        var request = _fixture.Build<CreateUserRequest>()
            .With(x => x.Name, "")
            .Create();
        // Act
        var response = _validator.TestValidate(request);
        
        // Assert
        response.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Имя пользователя обязательно.");
    }

    [Fact]
    public void Name_Should_HaveError_When_ExceedsMaxLength()
    {
        // Arrange
        var request = _fixture.Build<CreateUserRequest>()
            .With(x => x.Name, new string('A', 51))
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Имя не может быть длиннее 50 знаков.");
    }

    [Fact]
    public void Email_Should_HaveError_When_Empty()
    {
        // Arrange
        var request = _fixture.Build<CreateUserRequest>()
            .With(x => x.Email, "")
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Почта обязательна");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name+Label@sub.domain.xyz")]
    [InlineData("user@.com")]
    public void Email_Should_NotHaveError_When_IsValid(string validEmail)
    {
        // Arrange
        var request = _fixture.Build<CreateUserRequest>()
            .With(x => x.Email, validEmail)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("qwerty")]
    [InlineData("@missing.org")]
    [InlineData("missing-at.com")]
    public void Email_Should_HaveError_When_Invalid(string invalidEmail)
    {
        // Arrange
        var request = _fixture.Build<CreateUserRequest>()
            .With(x => x.Email, invalidEmail)
            .Create();
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Требуется действующий адрес почты");
    }

    [Fact]
    public void Should_HaveMultipleErrors_When_MultipleFieldsInvalid()
    {
        // Arrange
        var request = new CreateUserRequest(
            Name: "",
            Email: "",
            Balance: 0m);
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_NotHaveErrors_When_MultipleFieldsValid()
    {
        // Arrange
        var request = new CreateUserRequest(
            Name: "Test Name",
            Email: "test@email.com",
            Balance: 1m);
        // Act
        var result = _validator.TestValidate(request);
        
        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}