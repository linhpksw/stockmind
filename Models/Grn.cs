using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Grn
{
    public long GrnId { get; set; }

    public long? PoId { get; set; }

    public DateTime ReceivedAt { get; set; }

    public long? ReceiverId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<Grnitem> Grnitems { get; set; } = new List<Grnitem>();

    public virtual Po? Po { get; set; }

    public virtual UserAccount? Receiver { get; set; }
}
