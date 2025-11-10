namespace stockmind.DTOs.Waste
{
    public class WasteRequestDto
    {
        public string ProductId { get; set; } = string.Empty;

        public string LotId { get; set; } = string.Empty;

        public decimal Qty { get; set; }

        public string Reason { get; set; } = string.Empty;
    }
}
