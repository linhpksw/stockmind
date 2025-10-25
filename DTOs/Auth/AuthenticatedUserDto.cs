using System;
using System.Collections.Generic;

namespace stockmind.DTOs.Auth;

public class AuthenticatedUserDto
{
    public long UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public IReadOnlyCollection<string> Roles { get; set; } = Array.Empty<string>();
}
