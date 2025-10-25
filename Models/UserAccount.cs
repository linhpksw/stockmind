using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class UserAccount
{
    public long UserId { get; set; }

    public string Username { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    public string? PasswordHash { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<Grn> Grns { get; set; } = new List<Grn>();

    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    public virtual ICollection<Role> Roles { get; set; } = new List<Role>();
}
