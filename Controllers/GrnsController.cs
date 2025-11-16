using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Grns;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GrnsController : Controller
    {
        private readonly GrnService _grnService;
        public GrnsController(GrnService grnService)
        {
            _grnService = grnService;
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> CreateGrnAsync(
            [FromBody] CreateGrnRequestDto request,
            CancellationToken cancellationToken)
        {
            var grn = await _grnService.CreateGrnAsync(request, cancellationToken);
            return CreatedAtRoute(
                routeName: "GetGrnById",
                routeValues: new { id = grn.Id },
                value: new ResponseModel<GrnResponseDto>(grn)
            );
        }

        [HttpPost("sync")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> SyncGrnsAsync(
            [FromQuery] ListGrnQueryDto query,
            CancellationToken cancellationToken)
        {
            query.Normalize();
            var summaries = await _grnService.SyncFromOpenPurchaseOrdersAsync(query.PageNum, query.PageSize, cancellationToken);
            return Ok(summaries);
        }

        [HttpGet("summary")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER,STORE_STAFF")]
        public async Task<IActionResult> ListSummariesAsync(
            [FromQuery] ListGrnQueryDto query,
            CancellationToken cancellationToken)
        {
            query.Normalize();
            var summaries = await _grnService.ListSummariesAsync(query.PageNum, query.PageSize, cancellationToken);
            return Ok(summaries);
        }

        [HttpGet("{id}", Name = "GetGrnById")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> GetGrnById(long id, CancellationToken cancellationToken)
        {
            var grn = await _grnService.GetByIdAsync(id, cancellationToken);

            if (grn == null)
                return NotFound(new ResponseModel<string>("GRN not found"));

            return Ok(new ResponseModel<GrnResponseDto>(grn));
        }

        [HttpPost("po/{poId}/accept")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> AcceptPoAsync(long poId, CancellationToken cancellationToken)
        {
            var summary = await _grnService.AcceptPoAsync(poId, cancellationToken);
            return Ok(new ResponseModel<GrnSummaryDto>(summary));
        }

        [HttpPost("po/{poId}/cancel")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> CancelPoAsync(long poId, CancellationToken cancellationToken)
        {
            var summary = await _grnService.CancelPoAsync(poId, cancellationToken);
            return Ok(new ResponseModel<GrnSummaryDto>(summary));
        }
    }
}
