using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Notifications
{
    public class NotificationService(IUnitOfWork _unitOfWork) : INotificationService
    {
        public IEnumerable<NotificationDto> GetAllNotifications()
        {
            var messages = _unitOfWork.GetRepository<Notification>().GetAllAsIQueryable()
                .OrderByDescending(x => x.CreatedAt);

            var messageDtos = messages.Select(x => new NotificationDto
            {
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Message = x.Message,
                Type = x.Type.ToString(),
            }).ToList();
            return messageDtos;
        }
        public async Task<IEnumerable<int>> CreateLowStockNotification(List<TransactionItem> lowStockItems,List<Inventory> inventories)
        {

            List<Notification> notifications = new List<Notification>();

            foreach (var item in lowStockItems)
            {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.ProductId);
                if (inventory == null) { 
                    throw new Exception("no inventory found");
                }
                Notification notification = new Notification
                {
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Type = NotificationType.LowStockAlert,
                    Message = $"Product: {inventory.Product.Name} has reached the reorder level. Current quantity: {inventory.Quantity}",
                    UserId = 1 //TODO Auth
                };
                notifications.Add(notification);
                await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            }
                
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
            {
                throw new Exception("No notification is added");
                ///TODO exception handling
            }
            return notifications.Select(x => x.Id).ToList();
        }

        public async Task CreateDeadStockNotification()
        {

            var recentlySoldIds = _unitOfWork.GetRepository<TransactionItem>().GetAllAsIQueryable()
                .Where(ti => ti.Transaction.Type == TransactionType.Out && ti.Transaction.CreatedAt > DateTime.UtcNow.AddMonths(-3))
                ///TODO settings
                .Select(ti => ti.ProductId)
                .Distinct()
                .ToList();

            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Include(p => p.Product)
                .Include(p => p.Product.TransactionItems)
                .ThenInclude(ti => ti.Transaction)
                .Where(inv => !recentlySoldIds.Contains(inv.ProductId) && inv.BranchId == 1)
                .ToList();
  
            //foreach (var inventory in inventories)
            //{
               
            //    var lastTransactionItem = inventory.Product.TransactionItems
            //        .Where(ti => ti.Transaction.Type == TransactionType.Out)
            //        .OrderByDescending(ti => ti.Transaction.CreatedAt)
            //        .FirstOrDefault();

            //    string lastSellingDate;
            //    if (lastTransactionItem != null)
            //    {
                   
            //        lastSellingDate = NotificationServiceHelper.GenerateSinceDate(lastTransactionItem.Transaction.CreatedAt);
            //    }
            //    else
            //    {
                    
            //        lastSellingDate = "the beginning (No sales recorded yet)";
            //    }          
            //}
            Notification notification = new Notification
            {
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.DeadStockAlert,
                Message = $"There are {inventories.Count()} products have shown no sales activity since {3} months, Check them now.",
                ///TODO settings
                UserId = 1
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);

            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0 && inventories.Any())
                throw new Exception("error at dead notification add");
        }

        //public async Task<IEnumerable<int>> CreateDeadStockNotification(Transaction transaction)
        //{
        //    var outTransactions = _unitOfWork.GetRepository<Transaction>().GetAllAsIQueryable()
        //        .Where(x => x.Type == TransactionType.Out && x.CreatedAt <= DateTime.UtcNow.AddMonths(-3));

        //}
    }
}
