using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using System.Globalization;

namespace stockmind.Commons.Helpers
{
    public static class StockMovementCodeHelper
    {
        public static string ToPublicId(long movementId)
        {
            if (movementId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(movementId));
            }

            return movementId.ToString(CultureInfo.InvariantCulture);
        }

        public static long FromPublicId(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "movementId" });
            }

            var trimmed = publicId.Trim();

            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "movementId" });
            }

            return value;
        }
    }
}
