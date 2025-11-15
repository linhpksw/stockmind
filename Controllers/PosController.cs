using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Pos;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PosController : Controller
    {
        private readonly PoService _poService;

        public PosController(PoService poService)
        {
            _poService = poService;
        }

        #region Create

        [HttpPost]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER")]
        public async Task<IActionResult> CreatePoAsync(
            [FromBody] CreatePoRequestDto request,
            CancellationToken cancellationToken)
        {
            var po = await _poService.CreatePoAsync(request, cancellationToken);

            return CreatedAtRoute(
                routeName: "GetPoById",
                routeValues: new { id = po.Id },
                value: new ResponseModel<PoResponseDto>(po)
            );
        }

        #endregion

        [HttpPost("sync")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER")]
        public async Task<IActionResult> SyncPurchaseOrdersAsync(
            [FromQuery] ListPurchaseOrdersQueryDto query,
            CancellationToken cancellationToken)
        {
            query.Normalize();
            var orders = await _poService.SyncFromMasterDataAsync(query.PageNum, query.PageSize, cancellationToken);
            return Ok(orders);
        }

        [HttpGet("summary")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
        public async Task<IActionResult> ListSummariesAsync(
            [FromQuery] ListPurchaseOrdersQueryDto query,
            CancellationToken cancellationToken)
        {
            query.Normalize();
            var orders = await _poService.ListSummariesAsync(query.PageNum, query.PageSize, cancellationToken);
            return Ok(orders);
        }

        #region Get by id

        [HttpGet("{id}", Name = "GetPoById")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER")]
        public async Task<IActionResult> GetPoByIdAsync(
            [FromRoute] long id,
            CancellationToken cancellationToken)
        {
            var po = await _poService.GetPoByIdAsync(id, cancellationToken);
            return Ok(new ResponseModel<PoResponseDto>(po));
        }

        #endregion
    }
}
