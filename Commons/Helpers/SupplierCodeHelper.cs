using System;
using System.Globalization;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;

namespace stockmind.Commons.Helpers;

public static class SupplierCodeHelper
{
    public static string ToPublicId(long supplierId)
    {
        if (supplierId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(supplierId));
        }

        return supplierId.ToString(CultureInfo.InvariantCulture);
    }

    public static long FromPublicId(string? publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "supplierId" });
        }

        var trimmed = publicId.Trim();

        if (!long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            throw new BizException(ErrorCode4xx.InvalidInput, new[] { "supplierId" });
        }

        return value;
    }
}
