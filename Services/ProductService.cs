using stockmind.Commons.Errors;
using stockmind.Commons.Exceptions;
using stockmind.Commons.Helpers;
using stockmind.DTOs.Suppliers;
using stockmind.Models;
using stockmind.Repositories;

namespace stockmind.Services
{
    public class ProductService
    {
        private readonly ProductRepository _productRepository;
        private readonly ILogger<ProductService> _logger;
        public ProductService(ProductRepository productRepository, ILogger<ProductService> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

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

        #endregion

    }
}
