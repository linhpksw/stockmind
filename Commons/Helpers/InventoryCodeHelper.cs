using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using System.Globalization;

namespace stockmind.Commons.Helpers
{
    public static class InventoryCodeHelper
    {
        public static string ToPublicId(long inventoryId)
        {
            if (inventoryId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(inventoryId));
            }

            return inventoryId.ToString(CultureInfo.InvariantCulture);
        }
        public static long FromPublicId(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "inventoryId" });
            }

            var trimmed = publicId.Trim();

            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "inventoryId" });
            }

            return value;
        }
    }
}
