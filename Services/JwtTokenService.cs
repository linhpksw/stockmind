using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using stockmind.Commons.Configurations;
using stockmind.Models;

namespace stockmind.Services;

public class JwtTokenService
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenService(IOptions<JwtSettings> jwtOptions)
    {
        _jwtSettings = jwtOptions.Value;
    }

    #region Public APIs

    public TokenResult GenerateAccessToken(UserAccount user, IReadOnlyCollection<string> roles)
    {
        var utcNow = DateTime.UtcNow;
        var expiresAt = utcNow.AddMinutes(_jwtSettings.AccessTokenExpiryMinutes);

        var claims = BuildClaims(user, roles, utcNow);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jwtToken = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: utcNow,
            expires: expiresAt,
            signingCredentials: credentials);

        var handler = new JwtSecurityTokenHandler();
        var token = handler.WriteToken(jwtToken);

        return new TokenResult(token, expiresAt);
    }

    #endregion

    #region Internal helpers

    private static IEnumerable<Claim> BuildClaims(UserAccount user, IReadOnlyCollection<string> roles, DateTime issuedAt)
    {
        var baseClaims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(issuedAt).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            baseClaims.Add(new Claim("fullName", user.FullName));
        }

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            baseClaims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
        }

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            baseClaims.Add(new Claim("phoneNumber", user.PhoneNumber));
        }

        baseClaims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return baseClaims;
    }

    public sealed class TokenResult
    {
        public TokenResult(string accessToken, DateTime expiresAt)
        {
            AccessToken = accessToken;
            ExpiresAt = expiresAt;
        }

        public string AccessToken { get; }

        public DateTime ExpiresAt { get; }
    }

    #endregion
}
