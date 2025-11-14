using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Margins;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("api/margin-profiles")]
    public class MarginProfilesController : ControllerBase
    {
        private readonly MarginProfileService _marginProfileService;

        public MarginProfilesController(MarginProfileService marginProfileService)
        {
            _marginProfileService = marginProfileService;
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER,BUYER")]
        public async Task<IActionResult> ListAsync(CancellationToken cancellationToken)
        {
            var profiles = await _marginProfileService.ListAsync(cancellationToken);
            return Ok(new ResponseModel<IReadOnlyList<MarginProfileDto>>(profiles));
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> UpdateAsync(
            long id,
            [FromBody] UpdateMarginProfileRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _marginProfileService.UpdateAsync(id, request, cancellationToken);
            return Ok(new ResponseModel<MarginProfileDto>(result));
        }

        [HttpPost("import")]
        [Authorize(Roles = "ADMIN,INVENTORY_MANAGER")]
        public async Task<IActionResult> ImportAsync(
            [FromBody] ImportMarginProfilesRequestDto request,
            CancellationToken cancellationToken)
        {
            var result = await _marginProfileService.ImportAsync(request, cancellationToken);
            return Ok(new ResponseModel<ImportMarginProfilesResponseDto>(result));
        }
    }
}
