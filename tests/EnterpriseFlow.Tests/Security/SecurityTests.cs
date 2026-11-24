using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Infrastructure.Identity;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;

namespace EnterpriseFlow.Tests.Security;

public class SecurityTests
{
    private readonly PasswordHasher _passwordHasher = new();
    private readonly JwtTokenGenerator _jwtGenerator;

    public SecurityTests()
    {
        var options = Options.Create(new JwtSettings
        {
            Secret = "EnterpriseFlowSuperSecretKey2026!WithSufficientLengthForHmacSha256",
            Issuer = "EnterpriseFlowApi",
            Audience = "EnterpriseFlowClients",
            ExpiryMinutes = 15,
            RefreshTokenExpiryDays = 7
        });

        _jwtGenerator = new JwtTokenGenerator(options);
    }

    [Fact]
    public void PasswordHasher_ShouldHashAndVerifyCorrectly()
    {
        // Arrange
        const string rawPassword = "AdminPassword@2026!";

        // Act
        var hash = _passwordHasher.HashPassword(rawPassword);
        var isValid = _passwordHasher.VerifyPassword(rawPassword, hash);
        var isInvalid = _passwordHasher.VerifyPassword("WrongPassword", hash);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(rawPassword);
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public void JwtTokenGenerator_ShouldGenerateTokenWithClaims_AndReadBackPrincipal()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Jane Doe",
            Email = "jane.doe@enterpriseflow.internal",
            Role = UserRole.LegalApprover,
            IsActive = true
        };

        // Act
        var (token, jwtId, expiresAtUtc) = _jwtGenerator.GenerateAccessToken(user);
        var principal = _jwtGenerator.GetPrincipalFromExpiredToken(token);

        // Assert
        token.Should().NotBeNullOrWhiteSpace();
        jwtId.Should().NotBeNullOrWhiteSpace();
        expiresAtUtc.Should().BeAfter(DateTime.UtcNow);

        principal.Should().NotBeNull();
        principal!.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value.Should().Be("jane.doe@enterpriseflow.internal");
        principal.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value.Should().Be("LegalApprover");
    }

    [Fact]
    public void JwtTokenGenerator_GenerateRefreshToken_ShouldProduceCryptographicallySecureString()
    {
        // Act
        var token1 = _jwtGenerator.GenerateRefreshToken();
        var token2 = _jwtGenerator.GenerateRefreshToken();

        // Assert
        token1.Should().NotBeNullOrWhiteSpace();
        token2.Should().NotBeNullOrWhiteSpace();
        token1.Should().NotBe(token2);
        token1.Length.Should().BeGreaterThan(40);
    }
}
