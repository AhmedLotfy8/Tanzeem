using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Tanzeem.Services.Notifications
{
    public class NotificationService(IUnitOfWork _unitOfWork) : INotificationService
    {
        public async Task<PaginationResponseDto<NotificationDto>> GetAllNotifications(int page, int pageSize)
        {
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<Notification>().GetAllAsIQueryable()
                .OrderByDescending(x => x.CreatedAt);
                

            var rowsCount = query.Count();
            
            var messages = query.Skip((page - 1) * pageSize)
                .Take(pageSize);
            
            var messageDtos = await messages.Select(x => new NotificationDto
            {
                Id = x.Id,
                Title = x.Title,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt,
                Message = x.Message,
                Type = x.Type,
            }).ToListAsync();


            return new PaginationResponseDto<NotificationDto>()
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = rowsCount,
                Data = messageDtos
            };
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
        public async Task CreateDeadStockNotification(int branchId)
        {
            var recentlySoldIds = await _unitOfWork.GetRepository<TransactionItem>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Where(ti => ti.Transaction.Type == TransactionType.Out && ti.Transaction.CreatedAt > DateTime.UtcNow.AddMonths(-3)
                && ti.Transaction.BranchId == branchId)
                ///TODO settings
                /// TODO auth
                .Select(ti => ti.ProductId)
                .Distinct()
                .ToListAsync();

            var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Include(p => p.Product)
                .Include(p => p.Product.TransactionItems)
                .ThenInclude(ti => ti.Transaction)
                ///TODO auth
                .Where(inv => !recentlySoldIds.Contains(inv.ProductId) && inv.BranchId == branchId)
                .ToListAsync();

            if (!inventories.Any())
            {
                return;
            }

            Notification notification = new Notification
            {
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow,
                    Type = NotificationType.DeadStockAlert,
                    Message = $"There are {inventories.Count} products have shown no sales activity since {3} months, Check them now.",
                    ///TODO settings dead stock period
                    BranchId = branchId,
                    Title = "Dead Stock Alert"
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);

            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0 && inventories.Any())
                throw new Exception("error at dead notification add");
          
        }

        public async Task CreateExpiryNotification(int branchId)
        {
            var productsCount = await _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                .IgnoreQueryFilters() 
                .Where(p =>  p.ExpiryDate <= DateTime.UtcNow.AddMonths(3)
                 && p.Inventories.Any(inv => inv.BranchId == branchId && inv.Quantity > 0))
                .CountAsync();

            if (productsCount == 0)
            {
                return;
            }

            Notification notification = new Notification
            {
                IsRead = false,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.ExpiryAlert,
                Message = $"There are {productsCount} products near expiry, Check them now.",
                BranchId = branchId,
                Title = "Expiry Warning"
            };

            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new Exception("error at expiry notification add");
        }

        public async Task createLowStockNotificationWeekly(int branchId)
        {
            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Include(inv => inv.Product)
                .Where(inv => inv.BranchId == branchId
                && inv.Quantity > 0
                && inv.Quantity < inv.Product.ReorderLevel)
                .Count();
            if (inventories ==0 )
            {
                return;
            }

            Notification notification = new Notification()
            {
                Title = "Low Stock Alert",
                IsRead = false,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.LowStockAlert,
                Message = $"{inventories} products have reached the reorder level, Check them now."
            };
            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new Exception("error at expiry notification add");
        }
        public async Task createOutOfStockNotificationWeekly(int branchId)
        {
            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .IgnoreQueryFilters()
                .Include(inv => inv.Product)
                .Where(inv => inv.BranchId == branchId
                && inv.Quantity == 0)
                .Count();
            
            if (inventories == 0)
            {
                return;
            }
            Notification notification = new Notification()
            {
                Title = "Out of stock Alert",
                IsRead = false,
                BranchId = branchId,
                CreatedAt = DateTime.UtcNow,
                Type = NotificationType.OutOfStock,
                Message = $"{inventories} products is completely out of stock, Check them now."
            };
            await _unitOfWork.GetRepository<Notification>().AddAsync(notification);
            int affected = await _unitOfWork.SaveChangesAsync();
            if (affected <= 0)
                throw new Exception("error at expiry notification add");
        }
        public async Task CreateNotification()
        {
            List<int> branchIds = _unitOfWork.GetRepository<Branch>().GetAllAsIQueryable()
                .Select(br => br.Id)
                .Distinct()
                .ToList();
            foreach (var branchId in branchIds)
            {
                await CreateDeadStockNotification(branchId);
                await CreateExpiryNotification(branchId);
                await createLowStockNotificationWeekly(branchId);
                await createOutOfStockNotificationWeekly(branchId);
            }
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.GetRepository<Notification>().GetByIdAsync(notificationId);

            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                _unitOfWork.GetRepository<Notification>().UpdateAsync(notification);
                await _unitOfWork.SaveChangesAsync();
            }

            return true;
        }

        public async Task MarkAllAsReadAsync()
        {
            var unreadNotifications = await _unitOfWork.GetRepository<Notification>()
                .GetAllAsIQueryable()
                .Where(n => n.BranchId == 1 && !n.IsRead) ///TODO auth
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                    _unitOfWork.GetRepository<Notification>().UpdateAsync(notification);
                }
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}
