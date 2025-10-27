using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace stockmind.DTOs.Pos {
    public class CreatePoRequestDto{
        [Required]
        public long SupplierId { get; set; }

        [Required]
        [MinLength(1)]
        public List<PoItemDto> Items { get; set; }
    }
    public class PoItemDto {
        [Required]
        public long ProductId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
        public int Qty { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "Unit cost must be positive")]
        public decimal UnitCost { get; set; }

        [Required]
        public DateTime ExpectedDate { get; set; }
    }
}
