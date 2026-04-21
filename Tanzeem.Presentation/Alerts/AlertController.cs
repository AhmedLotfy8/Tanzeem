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
        public IActionResult GetAlerts(NotificationType? type)
        {
            var result = _alertService.ShowAlerts(type);
            return Ok(result);
        }
    }
}
