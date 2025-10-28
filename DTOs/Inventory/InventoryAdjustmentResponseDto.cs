using System.ComponentModel.DataAnnotations;

namespace stockmind.DTOs.Inventory
{
    public class InventoryAdjustmentResponseDto
    {
        public string MovementId { get; set; } = string.Empty;
        public string Type { get; set; } = "ADJUSTMENT";
        public decimal Qty { get; set; }
    }
}
