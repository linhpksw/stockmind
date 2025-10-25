using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Role
{
    public long RoleId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
}
