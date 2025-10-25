using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Role
{
    public long RoleId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<UserAccount> Users { get; set; } = new List<UserAccount>();
}
