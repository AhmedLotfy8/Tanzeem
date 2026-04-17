using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Notifications;

namespace Tanzeem.Services.Notifications
{
    public class NotificationService(IUnitOfWork _unitOfWork) : INotificationService
    {
        public int CreateLowStockNotification(Transaction transaction)
        {
            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable().Include(x => x.Product);

            foreach (var item in transaction.TransactionItems)
            {
                var inventory = inventories.FirstOrDefault(x => x.ProductId == item.ProductId && x.BranchId == 1);
                if (inventory != null)
                {
                    if (inventory.Quantity <= inventory.Product.ReorderLevel)
                    {

                    }
                }
            }

            return 1;//notify id
        }
    }
}
