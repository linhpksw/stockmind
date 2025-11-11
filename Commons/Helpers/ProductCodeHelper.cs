using System;
using System.Globalization;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;

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

        public static string ToPublicId(long productId)
        {
            if (productId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(productId));
            }

            return productId.ToString(CultureInfo.InvariantCulture);
        }
    }
}
