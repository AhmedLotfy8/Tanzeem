using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Abstractions.Alerts
{
    public interface IAlertService
    {
        public IEnumerable<AlertDto> ShowAlerts(NotificationType? type);
    }
}
