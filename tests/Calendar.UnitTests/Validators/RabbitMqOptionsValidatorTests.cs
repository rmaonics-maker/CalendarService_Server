using AutoFixture;
using Calendar_Service.Validators;
using Calendar.Api.Options;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace Calendar.UnitTests.Validators;

public class RabbitMqOptionsValidatorTests
{
    private readonly RabbitMqOptionsValidator _sut = new ();
    private readonly IFixture _fixture = new Fixture();
    
#region Host
    [Fact]
    public void Host_Should_HaveError_When_Empty()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.Host, string.Empty)
            .Create();
        // Act
        var result = _sut.TestValidate(options);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host)
            .WithErrorMessage("RabbitMQ host is required");
    }

    [Theory]
    [InlineData("  ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Host_Should_HaveError_When_WhiteSpaceOnly(string hostName)
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.Host, hostName)
            .Create();
        // Act
        var result = _sut.TestValidate(options);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host)
            .WithErrorMessage("RabbitMQ host is required");
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("rabbitmq.prod.example.com")]
    [InlineData("rabbitmq_server")]
    public void Host_Should_Pass_When_Valid(string validHost)
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.Host, validHost)
            .Create();
        // Act
        var result = _sut.TestValidate(options);
        
        // Assert
        result.ShouldNotHaveValidationErrorFor(x=>x.Host);
    }
#endregion
#region Username
    [Fact]
    public void Username_Should_HaveError_When_Empty()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x=>x.Username, string.Empty)
            .Create();
        // Act
        var result = _sut.TestValidate(options);
        
        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("RabbitMQ username is required");
    }
    
    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Username_Returns_Error_When_WhitespaceOnly(string whitespace)
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x=>x.Username, whitespace)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Username)
            .WithErrorMessage("RabbitMQ username is required");
    }

    [Theory]
    [InlineData("guest")]
    [InlineData("admin")]
    [InlineData("service_user")]
    [InlineData("service-user")]
    public void Username_WhenValid_Passes(string validUsername)
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x=>x.Username, validUsername)
            .Create();

        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }
#endregion
#region Port - SSL Disabled
    [Fact]
    public void Port_Should_Passes_When_SslDisabledAndPort5672()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.SslEnabled, false)
            .With(x => x.Port, 5672)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Theory]
    [InlineData(5671)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9999)]
    [InlineData(65535)]
    public void Port_Should_ReturnsError_When_SslDisabledAndWrongPort(int wrongPort)
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.SslEnabled, false)
            .With(x => x.Port, wrongPort)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port)
              .WithErrorMessage("Port must be 5671 if SSL is enabled, otherwise 5672");
    }
#endregion
#region Port - SSL Enabled
    [Fact]
    public void Port_Should_Passes_When_SslEnabledAndPort5671()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.SslEnabled, true)
            .With(x => x.Port, 5671)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Port);
    }

    [Theory]
    [InlineData(5672)]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(9999)]
    public void Port_Should_ReturnsError_When_SslEnabledAndWrongPort(int wrongPort)
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.SslEnabled, true)
            .With(x => x.Port, wrongPort)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Port)
              .WithErrorMessage("Port must be 5671 if SSL is enabled, otherwise 5672");
    }
#endregion
#region Combined Scenarios
    [Fact]
    public void Should_Passes_When_AllFieldsValid_SslDisabled()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.SslEnabled, false)
            .With(x => x.Port, 5672)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Should_Passes_When_AllFieldsValid_SslEnabled()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.SslEnabled, true)
            .With(x => x.Port, 5671)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }
#endregion
#region Multiple Errors
    [Fact]
    public void Should_ReturnsBothErrors_When_HostAndUsernameEmpty()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.Host, string.Empty)
            .With(x => x.Username, string.Empty)
            .With(x=>x.Port, 5671)
            .With(x=>x.SslEnabled, true)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Host);
        result.ShouldHaveValidationErrorFor(x => x.Username);
        result.Errors.Count.Should().Be(2);
    }

    [Fact]
    public void MultipleErrors_WhenAllInvalid_ReturnsThreeErrors()
    {
        // Arrange
        var options = _fixture.Build<RabbitMqOptions>()
            .With(x => x.Host, string.Empty)
            .With(x => x.Username, string.Empty)
            .With(x=>x.Port, 0)
            .Create();
        // Act
        var result = _sut.TestValidate(options);

        // Assert
        result.Errors.Count.Should().Be(3);
        result.Errors.Should().Contain(e => e.PropertyName == "Host");
        result.Errors.Should().Contain(e => e.PropertyName == "Username");
        result.Errors.Should().Contain(e => e.PropertyName == "Port");
    }
#endregion
}