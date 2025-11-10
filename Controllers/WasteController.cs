using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Waste;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("waste")]
    public class WasteController : ControllerBase
    {
        private readonly WasteService _wasteService;

        public WasteController(WasteService wasteService)
        {
            _wasteService = wasteService;
        }

        [HttpPost]
        public async Task<IActionResult> RecordWasteAsync(
            [FromBody] WasteRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var response = await _wasteService.DisposeExpiredStockAsync(request, cancellationToken);
            return StatusCode(StatusCodes.Status201Created, new ResponseModel<WasteResponseDto>(response));
        }
    }
}
