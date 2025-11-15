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
            [FromQuery] int days = 1,
            CancellationToken cancellationToken = default)
        {
            var recommendations = await _markdownService.GetRecommendationsAsync(days, cancellationToken);
            return Ok(new ResponseModel<List<MarkdownRecommendationDto>>(recommendations));
        }

        [HttpGet("rules")]
        public async Task<IActionResult> ListRulesAsync(CancellationToken cancellationToken = default)
        {
            var rules = await _markdownService.ListRulesAsync(cancellationToken);
            return Ok(new ResponseModel<List<MarkdownRuleDto>>(rules));
        }

        [HttpPost("rules")]
        public async Task<IActionResult> CreateRuleAsync(
            [FromBody] MarkdownRuleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var created = await _markdownService.CreateRuleAsync(request, cancellationToken);
            return Ok(new ResponseModel<MarkdownRuleDto>(created));
        }

        [HttpPut("rules/{ruleId:long}")]
        public async Task<IActionResult> UpdateRuleAsync(
            [FromRoute] long ruleId,
            [FromBody] MarkdownRuleRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var updated = await _markdownService.UpdateRuleAsync(ruleId, request, cancellationToken);
            return Ok(new ResponseModel<MarkdownRuleDto>(updated));
        }

        [HttpDelete("rules/{ruleId:long}")]
        public async Task<IActionResult> DeleteRuleAsync(
            [FromRoute] long ruleId,
            CancellationToken cancellationToken = default)
        {
            var deleted = await _markdownService.DeleteRuleAsync(ruleId, cancellationToken);
            return Ok(new ResponseModel<MarkdownRuleDto>(deleted));
        }

        [HttpPost("apply")]
        public async Task<IActionResult> ApplyMarkdownAsync(
            [FromBody] MarkdownApplyRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await _markdownService.ApplyMarkdownAsync(request, cancellationToken);
            return Ok(new ResponseModel<MarkdownApplyResponseDto>(result));
        }

        [HttpPost("apply/bulk")]
        public async Task<IActionResult> ApplyMarkdownsAsync(
            [FromBody] MarkdownApplyBulkRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var result = await _markdownService.ApplyMarkdownsAsync(request, cancellationToken);
            return Ok(new ResponseModel<MarkdownApplyBulkResponseDto>(result));
        }

        [HttpPost("decisions/{decisionId:long}/revert")]
        public async Task<IActionResult> RevertLotSaleDecisionAsync(long decisionId, CancellationToken cancellationToken = default)
        {
            await _markdownService.RevertLotSaleDecisionAsync(decisionId, cancellationToken);
            return Ok(new ResponseModel<bool>(true));
        }
    }
}
