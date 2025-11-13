using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Categories;
using stockmind.Services;

namespace stockmind.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
    public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
    {
        var nodes = await _categoryService.ListHierarchyAsync(cancellationToken);
        return Ok(new ResponseModel<IReadOnlyList<CategoryNodeDto>>(nodes));
    }

    [HttpPost("import")]
    [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
    public async Task<IActionResult> ImportAsync(
        [FromBody] ImportCategoriesRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _categoryService.ImportCategoriesAsync(request, cancellationToken);
        return Ok(new ResponseModel<ImportCategoriesResponseDto>(result));
    }
}
