using Microsoft.AspNetCore.Authorization;
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
        //[Authorize(Roles = "")]
        public async Task<IActionResult> GetNotifications([FromQuery(Name = "Page_Size")] int pageSize =20, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await _notificationService.GetAllNotifications(page,pageSize);
            return Ok(result);
        }

    }
}
