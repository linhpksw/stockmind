using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using stockmind.Commons.Configurations;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.DTOs.Auth;
using stockmind.Models;
using stockmind.Repositories;
using BCryptNet = BCrypt.Net.BCrypt;

namespace stockmind.Services;

public class AuthService
{
    private readonly AuthRepository _authRepository;
    private readonly JwtTokenService _jwtTokenService;
    private readonly ILogger<AuthService> _logger;
    private readonly int _accessTokenExpiryMinutes;

    public AuthService(
        AuthRepository authRepository,
        JwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtOptions,
        ILogger<AuthService> logger)
    {
        _authRepository = authRepository;
        _jwtTokenService = jwtTokenService;
        _logger = logger;
        _accessTokenExpiryMinutes = jwtOptions.Value.AccessTokenExpiryMinutes;
    }

#region Public APIs

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var missingFields = new List<string>(2);
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            missingFields.Add("username");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            missingFields.Add("password");
        }

        if (missingFields.Count > 0)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { string.Join(", ", missingFields) });
        }

        var normalizedUsername = request.Username.Trim();

        var user = await _authRepository.GetActiveUserWithRolesAsync(normalizedUsername, cancellationToken);
        if (user is null)
        {
            _logger.LogInformation("Login failed for username {Username}: user not found or inactive.", normalizedUsername);
            throw new BizAuthenticationException(ErrorCode4xx.Unauthorized);
        }

        if (!ValidatePassword(request.Password, user))
        {
            _logger.LogInformation("Login failed for username {Username}: invalid credentials.", normalizedUsername);
            throw new BizAuthenticationException(ErrorCode4xx.Unauthorized);
        }

        var roleCodes = user.Roles.Select(role => role.Code).ToList();
        var token = _jwtTokenService.GenerateAccessToken(user, roleCodes);

        return new LoginResponseDto
        {
            AccessToken = token.AccessToken,
            TokenType = "Bearer",
            ExpiresAt = token.ExpiresAt,
            ExpiresIn = (int)TimeSpan.FromMinutes(_accessTokenExpiryMinutes).TotalSeconds,
            User = MapToAuthenticatedUserDto(user, roleCodes)
        };
    }

#endregion

#region Private helpers

    private static bool ValidatePassword(string password, UserAccount user)
    {
        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return false;
        }

        try
        {
            return BCryptNet.Verify(password, user.PasswordHash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }

    private static AuthenticatedUserDto MapToAuthenticatedUserDto(UserAccount user, IEnumerable<string> roleCodes)
    {
        return new AuthenticatedUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Roles = roleCodes.ToArray()
        };
    }

#endregion
}
