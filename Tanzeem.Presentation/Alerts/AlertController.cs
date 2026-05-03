using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Alerts;

namespace Tanzeem.Presentation.Alerts
{
    [ApiController]
    [Route ("api/[controller]")]
    public class AlertController(IAlertService _alertService) : ControllerBase
    {
        [HttpGet]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> GetAlerts(NotificationType? type, [FromQuery(Name = "Page_Size")] int pageSize = 15, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await _alertService.ShowAlerts(type,page,pageSize);
            return Ok(result);
        }

        [HttpGet("mini_Alert_dashboard")]
        public async Task<IActionResult> CountsDashboard()
        {
            var result = await _alertService.Counts();
            return Ok(result);
        }
    }
}
