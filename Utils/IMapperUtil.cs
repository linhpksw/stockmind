using stockmind.DTOs.Inventory;
using stockmind.Models;

namespace stockmind.Utils
{
    public interface IMapperUtil
    {
        LotDto MapToLotDto(Lot e);
        MovementDto MapToMovementDto(StockMovement e);
    }

    // Utils/MapperUtil.cs
    public class MapperUtil : IMapperUtil
    {
        public LotDto MapToLotDto(Lot e)
        {
            return new LotDto(
                LotId: e.LotCode ?? e.LotId.ToString(),
                QtyOnHand: e.QtyOnHand,
                ReceivedAt: e.ReceivedAt,
                ExpiryDate: e.ExpiryDate
            );
        }

        public MovementDto MapToMovementDto(StockMovement e)
        {
            return new MovementDto(
                Id: e.MovementId.ToString(),
                Type: e.Type.ToString(), // or map enum to string
                Qty: e.Qty,
                LotId: e.LotId.ToString(),
                At: e.CreatedAt,
                RefType: e.RefType,
                RefId: e.RefId?.ToString()
            );
        }
    }
}
