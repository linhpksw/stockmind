using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using stockmind.Commons.Responses;
using stockmind.DTOs.Alert;
using stockmind.Services;

namespace stockmind.Controllers
{
    [Route("api/alerts")]
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
                _logger.LogInformation("GET api/alerts triggered.");
                var alerts = await _service.GetAlertsAsync(ct);
                return Ok(new ResponseModel<AlertsAggregateDto>(alerts));
            }
            catch (OperationCanceledException oce)
            {
                _logger.LogWarning(oce, "GetAlerts canceled by client.");
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
        }
    }
}
