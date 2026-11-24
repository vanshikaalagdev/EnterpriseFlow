using EnterpriseFlow.Application.Common.Interfaces;
using EnterpriseFlow.Application.DTOs;
using EnterpriseFlow.Domain.Entities;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Auth.Commands.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken) : IRequest<AuthResponseDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.AccessToken).NotEmpty().WithMessage("AccessToken is required.");
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("RefreshToken is required.");
    }
}

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, AuthResponseDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            throw new UnauthorizedAccessException("Invalid access token.");
        }

        var jti = principal.Claims.FirstOrDefault(c => c.Type == "jti")?.Value;

        var existingRefreshToken = await _context.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == request.RefreshToken, cancellationToken);

        if (existingRefreshToken == null)
        {
            throw new UnauthorizedAccessException("Refresh token does not exist.");
        }

        // Fraud detection: If the refresh token was already used, revoke all tokens for this user!
        if (existingRefreshToken.IsUsed)
        {
            var userTokens = await _context.RefreshTokens
                .Where(r => r.UserId == existingRefreshToken.UserId)
                .ToListAsync(cancellationToken);

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                token.RevokedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Security violation: Attempted reuse of rotated refresh token. All active sessions revoked.");
        }

        if (existingRefreshToken.IsRevoked)
        {
            throw new UnauthorizedAccessException("Refresh token has been revoked.");
        }

        if (existingRefreshToken.ExpiresAtUtc < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        if (!string.IsNullOrWhiteSpace(jti) && existingRefreshToken.JwtId != jti)
        {
            throw new UnauthorizedAccessException("Refresh token does not match access token ID.");
        }

        var user = existingRefreshToken.User;
        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User account is inactive.");
        }

        // Token rotation: mark old token as used
        existingRefreshToken.IsUsed = true;

        var (newAccessToken, newJwtId, expiresAtUtc) = _jwtTokenGenerator.GenerateAccessToken(user);
        var newRefreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var newRefreshToken = new EnterpriseFlow.Domain.Entities.RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = newRefreshTokenString,
            JwtId = newJwtId,
            IsUsed = false,
            IsRevoked = false,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        existingRefreshToken.ReplacedByToken = newRefreshTokenString;
        _context.RefreshTokens.Add(newRefreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        var userDto = new UserDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive, user.CreatedAtUtc);
        return new AuthResponseDto(newAccessToken, newRefreshTokenString, expiresAtUtc, userDto);
    }
}
