using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Services;

namespace stockmind.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertsController : ControllerBase
    {
        private readonly AlertsService _service;
        private readonly ILogger<AlertsController> _logger;

        public AlertsController(AlertsService service, ILogger<AlertsController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAlerts(CancellationToken ct)
        {
            try
            {
                var res = await _service.GetAlertsAsync(ct);
                return Ok(res);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("GetAlerts canceled by client.");
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while computing alerts");
                return Problem(detail: ex.Message, statusCode: 500);
            }
        }
    }
}
