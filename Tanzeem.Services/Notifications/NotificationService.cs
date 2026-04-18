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
                .OrderByDescending(x => x.CreatedAt).ToList();

            var messageDtos = messages.Select(x => new NotificationDto
            {
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Message = x.Message,
                Type = x.Type,
            });
            return messageDtos;
        }
        public async Task<IEnumerable<int>> CreateLowStockNotification(Transaction transaction)
        {
            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable().Include(x => x.Product);
            List<Notification> notifications = new List<Notification>();

            foreach (var item in transaction.TransactionItems)
            {
                var inventory = inventories.FirstOrDefault(x => x.ProductId == item.ProductId && x.BranchId == 1);
                if (inventory == null)
                {
                    throw new Exception("this inventory not found");
                    ///TODO exception handling
                }
               
                if (inventory.Quantity <= inventory.Product.ReorderLevel)
                {
                    Notification notification = new Notification {
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Type = NotificationType.LowStockAlert,
                        Message = $"Product: {inventory.Product.Name} has reached the reorder level. Current quantity: {inventory.Quantity}"
                    };
                    notifications.Add(notification);
                    await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
                }

            }
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
            {
                throw new Exception("No notification is added");
                ///TODO exception handling
            }
            var ids = notifications.Select(x => x.Id);
            return ids;
        }
    }
}
