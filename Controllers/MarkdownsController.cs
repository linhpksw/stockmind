using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Markdowns;
using stockmind.Services;

namespace stockmind.Controllers
{
    [ApiController]
    [Route("markdowns")]
    public class MarkdownsController : ControllerBase
    {
        private readonly MarkdownService _markdownService;

        public MarkdownsController(MarkdownService markdownService)
        {
            _markdownService = markdownService;
        }

        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendationsAsync(
            [FromQuery] int days = 3,
            CancellationToken cancellationToken = default)
        {
            var recommendations = await _markdownService.GetRecommendationsAsync(days, cancellationToken);
            return Ok(new ResponseModel<List<MarkdownRecommendationDto>>(recommendations));
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyMarkdownAsync(
            [FromBody] MarkdownApplyRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await _markdownService.ApplyMarkdownAsync(request, cancellationToken);
            return Ok(new ResponseModel<MarkdownApplyResponseDto>(result));
        }
    }
}
