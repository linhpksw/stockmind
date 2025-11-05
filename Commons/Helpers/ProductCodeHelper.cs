using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using System.Globalization;

namespace stockmind.Commons.Helpers
{
    public static class ProductCodeHelper
    {
        public static long FromPublicId(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "productId" });
            }

            var trimmed = publicId.Trim();

            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "productId" });
            }

            return value;
        }
    }
}
