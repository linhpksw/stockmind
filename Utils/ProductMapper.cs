using System.Globalization;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Product;
using stockmind.Models;

namespace stockmind.Utils
{
    public static class ProductMapper
    {
        public static ProductResponseDto ToResponse(Product product)
        {
            return new ProductResponseDto
            {
                Id = ProductCodeHelper.ToPublicId(product.ProductId),
                SkuCode = product.SkuCode,
                Name = product.Name,
                CategoryId = product.CategoryId?.ToString(CultureInfo.InvariantCulture),
                IsPerishable = product.IsPerishable,
                ShelfLifeDays = product.ShelfLifeDays,
                Uom = product.Uom,
                Price = product.Price,
                MinStock = product.MinStock,
                SupplierId = product.SupplierId.HasValue ? SupplierCodeHelper.ToPublicId(product.SupplierId.Value) : null,
                MediaUrl = product.MediaUrl,
                CategoryName = product.Category?.Name,
                BrandName = product.Supplier?.Name,
                CreatedAt = product.CreatedAt,
                LastModifiedAt = product.LastModifiedAt,
                UpdatedAt = product.LastModifiedAt
            };
        }
    }
}
