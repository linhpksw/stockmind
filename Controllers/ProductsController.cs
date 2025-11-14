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
            if (request is null)
            {
                return BadRequest(new ResponseModel<string>("Request body is required."));
            }

            var payload = request!;
            _logger.LogInformation("Creating new product with SKU {Sku}", payload.SkuCode);
            var product = await _productService.CreateProductAsync(payload, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<ProductResponseDto>(product));
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> UpdateProductAsync([FromRoute] string id, [FromBody] UpdateProductRequestDto request, CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(new ResponseModel<string>("Request body is required."));
            }

            _logger.LogInformation("Updating product {ProductId}", id);
            var payload = request!;
            var product = await _productService.UpdateProductAsync(id, payload, cancellationToken);
            return Ok(new ResponseModel<ProductResponseDto>(product));
        }

        [HttpGet("{id}")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> GetProductAsync([FromRoute] string id, CancellationToken cancellationToken)
        {
            var product = await _productService.GetProductAsync(id, cancellationToken);
            return Ok(new ResponseModel<ProductResponseDto>(product));
        }

        [HttpPost("import")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> ImportProductsAsync(
            [FromBody] ImportProductsRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null)
            {
                return BadRequest(new ResponseModel<string>("Request body is required."));
            }

            var result = await _productService.ImportProductsAsync(request!, cancellationToken);
            return Ok(new ResponseModel<ImportProductsResponseDto>(result));
        }

    }
}
