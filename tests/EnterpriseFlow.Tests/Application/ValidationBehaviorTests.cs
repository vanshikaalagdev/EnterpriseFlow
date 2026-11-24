using EnterpriseFlow.Application.Auth.Commands.RegisterUser;
using EnterpriseFlow.Application.Common.Behaviors;
using EnterpriseFlow.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Moq;
using Xunit;

namespace EnterpriseFlow.Tests.Application;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task ValidationBehavior_WithInvalidCommand_ShouldThrowValidationException()
    {
        // Arrange: Missing FullName, invalid Email, short password
        var command = new RegisterUserCommand("", "invalid-email", "123", (UserRole)999);
        var validator = new RegisterUserCommandValidator();
        var behavior = new ValidationBehavior<RegisterUserCommand, Unit>(new[] { validator });

        var nextMock = new Mock<RequestHandlerDelegate<Unit>>();

        // Act
        Func<Task> act = async () => await behavior.Handle(command, nextMock.Object, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ValidationException>();
        exception.Which.Errors.Should().Contain(e => e.PropertyName == "FullName");
        exception.Which.Errors.Should().Contain(e => e.PropertyName == "Email");
        exception.Which.Errors.Should().Contain(e => e.PropertyName == "Password");
        exception.Which.Errors.Should().Contain(e => e.PropertyName == "Role");
        nextMock.Verify(n => n(), Times.Never);
    }

    [Fact]
    public async Task ValidationBehavior_WithValidCommand_ShouldCallNextDelegate()
    {
        // Arrange
        var command = new RegisterUserCommand("Vanshika", "vanshika@example.com", "Password@123", UserRole.Requester);
        var validator = new RegisterUserCommandValidator();
        var behavior = new ValidationBehavior<RegisterUserCommand, Unit>(new[] { validator });

        var nextMock = new Mock<RequestHandlerDelegate<Unit>>();
        nextMock.Setup(n => n()).ReturnsAsync(Unit.Value);

        // Act
        var result = await behavior.Handle(command, nextMock.Object, CancellationToken.None);

        // Assert
        result.Should().Be(Unit.Value);
        nextMock.Verify(n => n(), Times.Once);
    }
}
