using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Products;
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

        // it creates out of stock notification else       
        public async Task<IEnumerable<int>> CreateLowStockNotification(List<TransactionItem> lowStockItems,List<Inventory> inventories)
        {

            List<Notification> notifications = new List<Notification>();

            foreach (var item in lowStockItems)
            {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.ProductId && inv.BranchId == 1);
                ///TODO auth
                if (inventory == null) { 
                    throw new Exception("no inventory found");
                }
                if (inventory.Quantity == 0)
                {
                    Notification notification = new Notification
                    {
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Type = NotificationType.OutOfStock,
                        Message = $"Product: {inventory.Product.Name} is completely out of stock",
                        Title = "Out Of Stock Alert",
                        BranchId = 1 //TODO Auth
                    };
                    notifications.Add(notification);
                    await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
                }
                else
                {
                    Notification notification = new Notification
                    {
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow,
                        Type = NotificationType.LowStockAlert,
                        Message = $"Product: {inventory.Product.Name} has reached the reorder level. Current quantity: {inventory.Quantity}",
                        Title = "Low Stock Alert",
                        BranchId = 1 //TODO Auth
                    };

                    notifications.Add(notification);
                    await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
                }
            }
            if (notifications.Any())
            {
                int affected = await _unitOfWork.SaveChangesAsync();
                if (affected <= 0)
                {
                    throw new Exception("No notification is added");
                    ///TODO exception handling
                }
            }
            return notifications.Select(x => x.Id).ToList();
        }
        public async Task CreateDeadStockNotification()
        {

            var recentlySoldIds = _unitOfWork.GetRepository<TransactionItem>().GetAllAsIQueryable()
                .Where(ti => ti.Transaction.Type == TransactionType.Out && ti.Transaction.CreatedAt > DateTime.UtcNow.AddMonths(-3)
                && ti.Transaction.BranchId == 1)
                ///TODO settings
                /// TODO auth
                .Select(ti => ti.ProductId)
                .Distinct()
                .ToList();

            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Include(p => p.Product)
                .Include(p => p.Product.TransactionItems)
                .ThenInclude(ti => ti.Transaction)
                ///TODO auth
                .Where(inv => !recentlySoldIds.Contains(inv.ProductId) && inv.BranchId == 1)
                .ToList();
  
            Notification notification = new Notification
            {
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.DeadStockAlert,
                Message = $"There are {inventories.Count()} products have shown no sales activity since {3} months, Check them now.",
                ///TODO settings dead stock period
                BranchId = 1, ///TODO auth
                Title = "Dead Stock Alert"
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);

            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0 && inventories.Any())
                throw new Exception("error at dead notification add");
        }

        public async Task CreateExpiryNotification()
        {
            var productsCount = _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
              .Include(p => p.Inventories)
              .Where(p => p.ExpiryDate <= DateTime.UtcNow.AddMonths(3)) ///TODO settings
                  .Count();
            ///TODO auth test

            if (productsCount == 0)
            {
                throw new Exception("No products");
            }

            Notification notification = new Notification
            {
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.ExpiryAlert,
                Message = $"There are {productsCount} products near expiry.",
                BranchId = 1, ///TODO auth 
                Title = "Expiry Warning"
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new Exception("error at expiry notification add");
        }

    }
}
