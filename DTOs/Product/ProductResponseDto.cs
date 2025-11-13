using System;

namespace stockmind.DTOs.Product
{
    public class ProductResponseDto
    {
        public string Id { get; set; } = string.Empty;

        public string SkuCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? CategoryId { get; set; }

        public bool IsPerishable { get; set; }

        public int? ShelfLifeDays { get; set; }

        public string Uom { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int MinStock { get; set; }

        public string? SupplierId { get; set; }

        public string? MediaUrl { get; set; }

        public string? CategoryName { get; set; }

        public string? BrandName { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastModifiedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
