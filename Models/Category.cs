using System;
using System.Collections.Generic;

namespace stockmind.Models;

public partial class Category
{
    public long CategoryId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public long? ParentCategoryId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime LastModifiedAt { get; set; }

    public bool Deleted { get; set; }

    public virtual ICollection<Category> InverseParentCategory { get; set; } = new List<Category>();

    public virtual ICollection<MarkdownRule> MarkdownRules { get; set; } = new List<MarkdownRule>();

    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
