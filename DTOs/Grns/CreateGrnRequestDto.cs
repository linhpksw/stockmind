using System.ComponentModel.DataAnnotations;

namespace stockmind.DTOs.Grns
{
    public class CreateGrnRequestDto
    {
        [Required]
        public long PoId { get; set; }
        public DateTime ReceivedAt { get; set; }
        public List<CreateGrnItemDto> Items { get; set; } = new();
    }

    public class CreateGrnItemDto
    {
        public long ProductId { get; set; }
        public decimal QtyReceived { get; set; }
        public decimal UnitCost { get; set; }
        public string LotCode { get; set; } = null!;
        public DateOnly? ExpiryDate { get; set; }
    }
}
