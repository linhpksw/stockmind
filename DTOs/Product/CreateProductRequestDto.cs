namespace stockmind.DTOs.Product
{
    public class CreateProductRequestDto
    {
        public string SkuCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? CategoryId { get; set; }

        public bool IsPerishable { get; set; }

        public int? ShelfLifeDays { get; set; }

        public string Uom { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int MinStock { get; set; }

        public int LeadTimeDays { get; set; }

        public string? SupplierId { get; set; }
    }
}
