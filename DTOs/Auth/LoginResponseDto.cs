using System;

namespace stockmind.DTOs.Auth;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public string TokenType { get; set; } = "Bearer";

    public DateTime ExpiresAt { get; set; }

    public int ExpiresIn { get; set; }

    public AuthenticatedUserDto User { get; set; } = new();
}
