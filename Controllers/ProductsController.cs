using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Linq;
using stockmind.Commons.Responses;
using stockmind.DTOs.Product;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsController : ControllerBase
    {
        private readonly ProductService _productService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ProductService productService, ILogger<ProductsController> logger)
        {
            _productService = productService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> ListProductsAsync(CancellationToken cancellationToken)
        {
            var products = await _productService.ListProductsAsync(cancellationToken);
            return Ok(new ResponseModel<List<ProductResponseDto>>(products.ToList()));
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> CreateProductAsync([FromBody] CreateProductRequestDto request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating new product with SKU {Sku}", request?.SkuCode);
            var product = await _productService.CreateProductAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<ProductResponseDto>(product));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> UpdateProductAsync([FromRoute] string id, [FromBody] UpdateProductRequestDto request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating product {ProductId}", id);
            var product = await _productService.UpdateProductAsync(id, request, cancellationToken);
            return Ok(new ResponseModel<ProductResponseDto>(product));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> GetProductAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductAsync(id, cancellationToken);
            return Ok(new ResponseModel<ProductResponseDto>(product));
        }

    }
}
