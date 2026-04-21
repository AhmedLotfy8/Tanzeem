using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Abstractions.Notifications
{
    public interface INotificationService
    {
        public IEnumerable<NotificationDto> GetAllNotifications();
        public Task<IEnumerable<int>> CreateLowStockNotification(List<TransactionItem> transactionItems,List<Inventory> inventories);
        public Task CreateDeadStockNotification();

    }
}
