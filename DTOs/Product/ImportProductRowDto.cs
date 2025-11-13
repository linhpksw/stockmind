namespace stockmind.DTOs.Product
{
    public class ImportProductRowDto
    {
        public string? ProductId { get; set; }

        public string SkuCode { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Uom { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        public string? MediaUrl { get; set; }

        public string? CategoryName { get; set; }

        public string? BrandName { get; set; }

        public bool? IsPerishable { get; set; }

        public int? ShelfLifeDays { get; set; }

        public int? MinStock { get; set; }

        public int? LeadTimeDays { get; set; }
    }
}
