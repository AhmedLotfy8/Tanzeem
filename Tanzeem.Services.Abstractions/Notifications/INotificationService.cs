using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Services.Abstractions.Notifications
{
    public interface INotificationService
    {
        public int CreateLowStockNotification(Transaction transaction);
    }
}
