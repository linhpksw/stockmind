using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using System.Globalization;

namespace stockmind.Commons.Helpers
{
    public static class LotCodeHelper
    {
        public static long FromPublicId(string? publicId)
        {
            if (string.IsNullOrWhiteSpace(publicId))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lotId" });
            }

            var trimmed = publicId.Trim();

            if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "lotId" });
            }

            return value;
        }
    }
}
