using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Notifications;

namespace Tanzeem.Presentation.Notifications
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController(INotificationService _notificationService) : ControllerBase
    {
        [HttpGet]
        public IActionResult GetNotifications()
        {
            var result = _notificationService.GetAllNotifications();
            return Ok(result);
        }
    }
}
