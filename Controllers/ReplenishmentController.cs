using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.ReplenishmentSuggestion;
using stockmind.DTOs.Suppliers;
using stockmind.Models;
using stockmind.Services;


namespace stockmind.Controllers
{
    [ApiController]
    [Route("replenishments")]
    public class ReplenishmentController : Controller
    {
        private readonly ReplenishmentService _service;
        public ReplenishmentController(ReplenishmentService service)
        {
            _service = service;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> GetSuggestionsAsync(CancellationToken cancellationToken)
        {
            var result = await _service.GetSuggestionsAsync(cancellationToken);
            return Ok(new ResponseModel<List<ReplenishmentSuggestionDto>>(result));
        }
    }
}
