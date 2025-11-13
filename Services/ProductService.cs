using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;
using stockmind.Commons.Attributes;
using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Product;
using stockmind.Models;
using stockmind.Repositories;
using stockmind.Utils;

namespace stockmind.Services
{
    public class ProductService
    {
        private readonly ProductRepository _productRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly SupplierRepository _supplierRepository;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            ProductRepository productRepository,
            CategoryRepository categoryRepository,
            SupplierRepository supplierRepository,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Create

        [Transactional]
        public async Task<ProductResponseDto> CreateProductAsync(CreateProductRequestDto request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();
            ValidateProductRequest(request.SkuCode, request.Name, request.Uom, request.Price, request.MinStock, request.LeadTimeDays, request.IsPerishable, request.ShelfLifeDays);

            var normalizedSku = request.SkuCode.Trim();
            var normalizedName = request.Name.Trim();
            var normalizedUom = request.Uom.Trim().ToUpperInvariant();

            if (await _productRepository.ExistsBySkuAsync(normalizedSku, null, cancellationToken))
            {
                throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedSku });
            }

            if (await _productRepository.ExistsByNameAsync(normalizedName, null, cancellationToken))
            {
                throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedName });
            }

            var categoryId = await ResolveCategoryIdAsync(request.CategoryId, cancellationToken);
            var supplierId = await ResolveSupplierIdAsync(request.SupplierId, cancellationToken);

            var product = new Product
            {
                SkuCode = normalizedSku,
                Name = normalizedName,
                CategoryId = categoryId,
                IsPerishable = request.IsPerishable,
                ShelfLifeDays = request.IsPerishable ? request.ShelfLifeDays : null,
                Uom = normalizedUom,
                Price = request.Price,
                MinStock = request.MinStock,
                LeadTimeDays = request.LeadTimeDays,
                SupplierId = supplierId,
                MediaUrl = NormalizeMediaUrl(request.MediaUrl),
                Deleted = false
            };

            await _productRepository.AddAsync(product, cancellationToken);

            _logger.LogInformation("Created product {Sku} with id {ProductId}", product.SkuCode, product.ProductId);
            return ProductMapper.ToResponse(product);
        }

        #endregion

        #region Update

        [Transactional]
        public async Task<ProductResponseDto> UpdateProductAsync(string publicId, UpdateProductRequestDto request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            cancellationToken.ThrowIfCancellationRequested();
            ValidateProductRequest(request.SkuCode, request.Name, request.Uom, request.Price, request.MinStock, request.LeadTimeDays, request.IsPerishable, request.ShelfLifeDays);

            var productId = ProductCodeHelper.FromPublicId(publicId);
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
                          ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            if (product.Deleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
            }

            var normalizedSku = request.SkuCode.Trim();
            if (!string.Equals(product.SkuCode, normalizedSku, StringComparison.OrdinalIgnoreCase))
            {
                if (await _productRepository.ExistsBySkuAsync(normalizedSku, product.ProductId, cancellationToken))
                {
                    throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedSku });
                }
                product.SkuCode = normalizedSku;
            }

            var normalizedName = request.Name.Trim();
            if (!string.Equals(product.Name, normalizedName, StringComparison.OrdinalIgnoreCase))
            {
                if (await _productRepository.ExistsByNameAsync(normalizedName, product.ProductId, cancellationToken))
                {
                    throw new BizDataAlreadyExistsException(ErrorCode4xx.DataAlreadyExists, new[] { normalizedName });
                }
                product.Name = normalizedName;
            }

            product.CategoryId = await ResolveCategoryIdAsync(request.CategoryId, cancellationToken);
            product.IsPerishable = request.IsPerishable;
            product.ShelfLifeDays = request.IsPerishable ? request.ShelfLifeDays : null;
            product.Uom = request.Uom.Trim().ToUpperInvariant();
            product.Price = request.Price;
            product.MinStock = request.MinStock;
            product.LeadTimeDays = request.LeadTimeDays;
            product.SupplierId = await ResolveSupplierIdAsync(request.SupplierId, cancellationToken);
            product.MediaUrl = NormalizeMediaUrl(request.MediaUrl);

            await _productRepository.UpdateAsync(product, cancellationToken);

            _logger.LogInformation("Updated product {ProductId}", product.ProductId);
            return ProductMapper.ToResponse(product);
        }

        #endregion

        #region Get by id

        public async Task<Product> GetProductByIdAsync(string publicId, bool includeDeleted, CancellationToken cancellationToken)
        {
            var productId = ProductCodeHelper.FromPublicId(publicId);

            var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
                             ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });

            if (product.Deleted && !includeDeleted)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { publicId });
            }

            return product;
        }

        public async Task<ProductResponseDto> GetProductAsync(string publicId, CancellationToken cancellationToken)
        {
            var product = await GetProductByIdAsync(publicId, includeDeleted: false, cancellationToken);
            EnsurePerishableIntegrity(product);
            return ProductMapper.ToResponse(product);
        }

        #endregion

        #region List

        public async Task<IReadOnlyList<ProductResponseDto>> ListProductsAsync(CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);
            foreach (var product in products)
            {
                EnsurePerishableIntegrity(product);
            }

            return products.Select(ProductMapper.ToResponse).ToList();
        }

        #endregion


        #region Import

        public async Task<ImportProductsResponseDto> ImportProductsAsync(
            ImportProductsRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var response = new ImportProductsResponseDto();
            if (request.Rows == null || request.Rows.Count == 0)
            {
                return response;
            }

            var trackedProducts = await _productRepository.ListAllTrackedAsync(includeDeleted: true, cancellationToken);
            var productsBySku = trackedProducts
                .Where(p => !string.IsNullOrWhiteSpace(p.SkuCode))
                .ToDictionary(p => p.SkuCode.Trim(), p => p, StringComparer.OrdinalIgnoreCase);
            var productsById = trackedProducts.ToDictionary(p => p.ProductId);

            var categories = await _categoryRepository.ListAllTrackedAsync(includeDeleted: false, cancellationToken);
            var categoriesByName = categories
                .Where(c => !string.IsNullOrWhiteSpace(c.Name))
                .ToDictionary(c => c.Name.Trim(), c => c, StringComparer.OrdinalIgnoreCase);
            var categoriesByCode = categories
                .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                .ToDictionary(c => c.Code.Trim(), c => c, StringComparer.OrdinalIgnoreCase);
            var categoriesById = categories.ToDictionary(c => c.CategoryId);

            var suppliers = await _supplierRepository.ListAllAsync(includeDeleted: false, cancellationToken);
            var suppliersByName = suppliers
                .Where(s => !string.IsNullOrWhiteSpace(s.Name))
                .ToDictionary(s => s.Name.Trim(), s => s, StringComparer.OrdinalIgnoreCase);

            var now = DateTime.UtcNow;
            var newProducts = new List<Product>();

            foreach (var row in request.Rows)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedSku = row?.SkuCode?.Trim();
                var normalizedName = row?.Name?.Trim();
                var normalizedUom = row?.Uom?.Trim();

                if (string.IsNullOrWhiteSpace(normalizedSku) ||
                    string.IsNullOrWhiteSpace(normalizedName) ||
                    string.IsNullOrWhiteSpace(normalizedUom) ||
                    row?.Price is null)
                {
                    response.SkippedInvalid += 1;
                    continue;
                }

                var price = row.Price.Value;
                if (price < 0)
                {
                    response.SkippedInvalid += 1;
                    continue;
                }

                long? categoryId = null;
                if (!string.IsNullOrWhiteSpace(row.CategoryName))
                {
                    if (!TryResolveCategoryId(
                            row.CategoryName,
                            categoriesByName,
                            categoriesByCode,
                            categoriesById,
                            out var resolvedCategoryId))
                    {
                        response.SkippedMissingCategory += 1;
                        continue;
                    }

                    categoryId = resolvedCategoryId;
                }

                long? supplierId = null;
                if (!string.IsNullOrWhiteSpace(row.BrandName))
                {
                    var normalizedBrand = row.BrandName.Trim();
                    if (suppliersByName.TryGetValue(normalizedBrand, out var supplier))
                    {
                        supplierId = supplier.SupplierId;
                    }
                    else
                    {
                        response.SkippedMissingSupplier += 1;
                        continue;
                    }
                }

                var product = ResolveProduct(row, normalizedSku, productsById, productsBySku);
                var normalizedUomUpper = normalizedUom!.ToUpperInvariant();
                var mediaUrl = NormalizeMediaUrl(row.MediaUrl);
                var targetIsPerishable = row.IsPerishable ?? product?.IsPerishable ?? false;
                var targetShelfLife = targetIsPerishable
                    ? row.ShelfLifeDays ?? product?.ShelfLifeDays
                    : null;
                var targetMinStock = row.MinStock ?? product?.MinStock ?? 0;
                var targetLeadTime = row.LeadTimeDays ?? product?.LeadTimeDays ?? 0;

                if (product is null)
                {
                    var newProduct = new Product
                    {
                        SkuCode = normalizedSku!,
                        Name = normalizedName!,
                        CategoryId = categoryId,
                        IsPerishable = targetIsPerishable,
                        ShelfLifeDays = targetShelfLife,
                        Uom = normalizedUomUpper,
                        Price = price,
                        MediaUrl = mediaUrl,
                        MinStock = targetMinStock,
                        LeadTimeDays = targetLeadTime,
                        SupplierId = supplierId,
                        Deleted = false,
                        CreatedAt = now,
                        LastModifiedAt = now
                    };

                    newProducts.Add(newProduct);
                    productsBySku[normalizedSku!] = newProduct;
                    response.Created += 1;
                    continue;
                }

                var changed = false;

                if (!string.Equals(product.Name, normalizedName, StringComparison.Ordinal))
                {
                    product.Name = normalizedName!;
                    changed = true;
                }

                if (!string.Equals(product.Uom, normalizedUomUpper, StringComparison.Ordinal))
                {
                    product.Uom = normalizedUomUpper;
                    changed = true;
                }

                if (product.Price != price)
                {
                    product.Price = price;
                    changed = true;
                }

                if (product.CategoryId != categoryId)
                {
                    product.CategoryId = categoryId;
                    changed = true;
                }

                if (product.SupplierId != supplierId)
                {
                    product.SupplierId = supplierId;
                    changed = true;
                }

                if (product.IsPerishable != targetIsPerishable)
                {
                    product.IsPerishable = targetIsPerishable;
                    changed = true;
                }

                if (product.ShelfLifeDays != targetShelfLife)
                {
                    product.ShelfLifeDays = targetShelfLife;
                    changed = true;
                }

                if (product.MinStock != targetMinStock)
                {
                    product.MinStock = targetMinStock;
                    changed = true;
                }

                if (product.LeadTimeDays != targetLeadTime)
                {
                    product.LeadTimeDays = targetLeadTime;
                    changed = true;
                }

                if (!string.Equals(product.MediaUrl, mediaUrl, StringComparison.Ordinal))
                {
                    product.MediaUrl = mediaUrl;
                    changed = true;
                }

                if (product.Deleted)
                {
                    product.Deleted = false;
                    changed = true;
                }

                if (changed)
                {
                    product.LastModifiedAt = now;
                    response.Updated += 1;
                }
            }

            if (newProducts.Count > 0)
            {
                await _productRepository.AddRangeAsync(newProducts, cancellationToken);
            }

            await _productRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product import completed: {Created} created, {Updated} updated, {Invalid} invalid, {MissingCategory} missing category, {MissingSupplier} missing supplier.",
                response.Created,
                response.Updated,
                response.SkippedInvalid,
                response.SkippedMissingCategory,
                response.SkippedMissingSupplier);

            return response;
        }

        #endregion

        #region Helpers

        private static void ValidateProductRequest(string skuCode, string name, string uom, decimal price, int minStock, int leadTimeDays, bool isPerishable, int? shelfLifeDays)
        {
            if (string.IsNullOrWhiteSpace(skuCode))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "skuCode" });
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "name" });
            }

            if (string.IsNullOrWhiteSpace(uom))
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "uom" });
            }

            if (price < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "price" });
            }

            if (minStock < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "minStock" });
            }

            if (leadTimeDays < 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "leadTimeDays" });
            }

            if (isPerishable)
            {
                if (!shelfLifeDays.HasValue || shelfLifeDays.Value <= 0)
                {
                    throw new BizException(ErrorCode4xx.InvalidInput, new[] { "shelfLifeDays" });
                }
            }
        }

        private async Task<long?> ResolveCategoryIdAsync(string? categoryId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(categoryId))
            {
                return null;
            }

            var trimmed = categoryId.Trim();

            if (long.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId))
            {
                var category = await _categoryRepository.GetByIdAsync(numericId, cancellationToken)
                               ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "categoryId" });
                return category.CategoryId;
            }

            var byCode = await _categoryRepository.GetByCodeAsync(trimmed, cancellationToken)
                         ?? throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "categoryId" });

            return byCode.CategoryId;
        }

        private async Task<long?> ResolveSupplierIdAsync(string? supplierId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(supplierId))
            {
                return null;
            }

            var supplierDbId = SupplierCodeHelper.FromPublicId(supplierId);
            var exists = await _supplierRepository.ExistsByIdAsync(supplierDbId, cancellationToken);

            if (!exists)
            {
                throw new BizNotFoundException(ErrorCode4xx.NotFound, new[] { "supplierId" });
            }

            return supplierDbId;
        }

        private static Product? ResolveProduct(
            ImportProductRowDto row,
            string? normalizedSku,
            IReadOnlyDictionary<long, Product> productsById,
            IReadOnlyDictionary<string, Product> productsBySku)
        {
            if (!string.IsNullOrWhiteSpace(row.ProductId))
            {
                try
                {
                    var internalId = ProductCodeHelper.FromPublicId(row.ProductId.Trim());
                    if (productsById.TryGetValue(internalId, out var productById))
                    {
                        return productById;
                    }
                }
                catch
                {
                    // Ignore invalid public IDs and fall back to SKU match.
                }
            }

            if (!string.IsNullOrWhiteSpace(normalizedSku) &&
                productsBySku.TryGetValue(normalizedSku, out var productBySku))
            {
                return productBySku;
            }

            return null;
        }

        private static bool TryResolveCategoryId(
            string rawValue,
            IReadOnlyDictionary<string, Category> categoriesByName,
            IReadOnlyDictionary<string, Category> categoriesByCode,
            IReadOnlyDictionary<long, Category> categoriesById,
            out long categoryId)
        {
            var normalized = rawValue.Trim();

            if (categoriesByName.TryGetValue(normalized, out var byName))
            {
                categoryId = byName.CategoryId;
                return true;
            }

            if (categoriesByCode.TryGetValue(normalized, out var byCode))
            {
                categoryId = byCode.CategoryId;
                return true;
            }

            if (long.TryParse(normalized, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numericId) &&
                categoriesById.TryGetValue(numericId, out var byId))
            {
                categoryId = byId.CategoryId;
                return true;
            }

            categoryId = default;
            return false;
        }

        private static string? NormalizeMediaUrl(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void EnsurePerishableIntegrity(Product product)
        {
            if (!product.IsPerishable)
            {
                return;
            }

            if (!product.ShelfLifeDays.HasValue || product.ShelfLifeDays.Value <= 0)
            {
                throw new BizException(ErrorCode4xx.InvalidInput, new[] { "shelfLifeDays" });
            }
        }

        #endregion
    }
}
