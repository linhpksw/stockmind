namespace stockmind.DTOs.Waste
{
    public class WasteResponseDto
    {
        public string MovementId { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public decimal Qty { get; set; }
    }
}
