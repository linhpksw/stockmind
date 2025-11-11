using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
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
        private static readonly JsonSerializerOptions AuditSerializerOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        private readonly ProductRepository _productRepository;
        private readonly CategoryRepository _categoryRepository;
        private readonly SupplierRepository _supplierRepository;
        private readonly ProductAuditLogRepository _productAuditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<ProductService> _logger;

        public ProductService(
            ProductRepository productRepository,
            CategoryRepository categoryRepository,
            SupplierRepository supplierRepository,
            ProductAuditLogRepository productAuditLogRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<ProductService> logger)
        {
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
            _supplierRepository = supplierRepository ?? throw new ArgumentNullException(nameof(supplierRepository));
            _productAuditLogRepository = productAuditLogRepository ?? throw new ArgumentNullException(nameof(productAuditLogRepository));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
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
                Deleted = false
            };

            await _productRepository.AddAsync(product, cancellationToken);
            await WriteAuditLogAsync(product.ProductId, "CREATE", CaptureSnapshot(product), cancellationToken);

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

            var before = CaptureSnapshot(product);

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

            await _productRepository.UpdateAsync(product, cancellationToken);

            var after = CaptureSnapshot(product);
            await WriteAuditLogAsync(product.ProductId, "UPDATE", new { Before = before, After = after }, cancellationToken);

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

        private static object CaptureSnapshot(Product product)
        {
            return new
            {
                product.SkuCode,
                product.Name,
                product.CategoryId,
                product.IsPerishable,
                product.ShelfLifeDays,
                product.Uom,
                product.Price,
                product.MinStock,
                product.LeadTimeDays,
                product.SupplierId
            };
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

        private async Task WriteAuditLogAsync(long productId, string action, object payload, CancellationToken cancellationToken)
        {
            var entry = new ProductAuditLog
            {
                ProductId = productId,
                Action = action,
                Actor = ResolveActor(),
                Payload = JsonSerializer.Serialize(payload, AuditSerializerOptions),
                CreatedAt = DateTime.UtcNow
            };

            await _productAuditLogRepository.AddAsync(entry, cancellationToken);
        }

        private string ResolveActor()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.Identity?.Name
                       ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? "system";
            }

            return "system";
        }

        #endregion
    }
}
