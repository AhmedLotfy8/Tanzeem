using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Notifications;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Alerts
{
    
    public class AlertService(IUnitOfWork _unitOfWork,ICurrentService _currentService) : IAlertService
    {
        public async Task<PaginationResponseDto<AlertDto>> ShowAlerts(
        NotificationType? type, int page, int pageSize, int ExpiryFilterByMonths = 3
            , int DeadStockFilterByMonths = 3)
        {
            if (page <= 0) page = 1;
            if (pageSize > 20) pageSize = 20;

            switch (type)
            {
                case NotificationType.LowStockAlert:
                    var lowData = await ShowLowStockAlerts();
                    return lowData.ToPaginatedResponse(page, pageSize);

                case NotificationType.DeadStockAlert:
                    var deadStockData = await ShowDeadStockAlerts(DeadStockFilterByMonths);
                    return deadStockData.ToPaginatedResponse(page, pageSize);

                case NotificationType.ExpiryAlert:
                    var expiryData = await ShowExpiryAlerts(ExpiryFilterByMonths);
                    return expiryData.ToPaginatedResponse(page, pageSize);

                case NotificationType.OutOfStock:
                    var outData = await ShowOutStockAlerts();
                    return outData.ToPaginatedResponse(page, pageSize);

                case NotificationType.OrderUpdate:
                    var orderData = await ShowOrderUpdates();
                     return orderData.ToPaginatedResponse(page, pageSize);

                default:
                    var lowAlerts = await ShowLowStockAlerts();
                    var deadAlerts = await ShowDeadStockAlerts(DeadStockFilterByMonths);
                    var expiryAlerts = await ShowExpiryAlerts(ExpiryFilterByMonths);
                    var outAlerts = await ShowOutStockAlerts();
                    var orderAlerts = await ShowOrderUpdates();

                    var allAlerts = lowAlerts
                        .Concat(deadAlerts)
                        .Concat(expiryAlerts)
                        .Concat(outAlerts)
                        .Concat(orderAlerts)
                        .OrderBy(x => x.ProductId)
                        .ToList();

                    return allAlerts.ToPaginatedResponse(page, pageSize);
            }
        }


        public async Task<IEnumerable<AlertDto>> ShowDeadStockAlerts(int DeadStockFilterByMonths =3)
        {

            int branchId = 1;
            //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var recentlySoldIds = _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.CreatedAt > DateTime.UtcNow.AddMonths(- DeadStockFilterByMonths)
                          && ti.Transaction.BranchId == branchId)
                .Select(ti => ti.ProductId)
                .Distinct();

            var rawQuery = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Where(inv => inv.BranchId == branchId
                           && inv.Quantity > 0
                           && !recentlySoldIds.Contains(inv.ProductId))
                .Select(inv => new
                {
                    inv.ProductId,
                    inv.Product.Name,
                    inv.Product.SKU,
                    LastSaleDate = inv.Product.TransactionItems
                        .Where(ti => ti.Transaction.Type == TransactionType.Out && ti.Transaction.BranchId == branchId)
                        .Select(ti => (DateTime?)ti.Transaction.CreatedAt)
                        .Max() 
                }).ToListAsync();

            var alerts = rawQuery
                .Select(x => new AlertDto
                {
                    AlertTitle = "Dead Stock Alert",
                    AlertDescription = $"{x.Name} has not moved in " +
                                       (x.LastSaleDate.HasValue
                                           ? NotificationServiceHelper.GenerateSinceDate(x.LastSaleDate.Value)
                                           : "No sales recorded yet"),
                    //AlertDescription = $"{x.Name} has not moved in " + x.LastSaleDate,
                    AlertSubTitle = $"{x.Name} (SKU: {x.SKU})",
                    ProductId = x.ProductId,
                    Type = NotificationType.DeadStockAlert,
                    Priority = AlertPriority.Critical.ToString(),
                }).OrderBy(x => x.ProductId);
            return alerts;
        }
        public async Task<IEnumerable<AlertDto>> ShowLowStockAlerts()
        {
            int branchId = 1;
            //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var alerts = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Where(x => x.BranchId == branchId
                && x.Quantity > 0
                && x.Quantity <= x.Product.ReorderLevel)
                .OrderBy(x => x.ProductId)
                .Select(inventory => new AlertDto
                {
                    AlertTitle = "Low Stock Alert",
                    AlertDescription = $"{inventory.Product.Name} stock is below minimum threshold",
                    AlertSubTitle = $"{inventory.Product.Name}(SKU: {inventory.Product.SKU}), Current Quantity: {inventory.Quantity}",
                    ProductId = inventory.ProductId,
                    Type = NotificationType.LowStockAlert,
                    Priority = AlertPriority.Warning.ToString(),
                }).ToListAsync();
            return alerts;         
        }
          
        public async Task<IEnumerable<AlertDto>> ShowExpiryAlerts(int ExpiryFilterByMonths = 3)
        {
            int companyId = 14;
            //int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            var products = await _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                .Where(p => p.ExpiryDate <= DateTime.UtcNow.AddMonths(ExpiryFilterByMonths)
                && p.CompanyId == companyId
                && p.Inventories.Any(i => i.Quantity > 0))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.SKU,
                    p.ExpiryDate
                })
                .ToListAsync();

            var alerts = products.Select(product =>
            {
                bool isExpired = product.ExpiryDate <= DateTime.UtcNow;

                return new AlertDto
                {
                    AlertTitle = isExpired ? "Expired Product" : "Expiry Warning",
                    AlertDescription = isExpired
                        ? $"{product.Name} has already expired!"
                        : $"{product.Name} will expire in {NotificationServiceHelper.GenerateSinceDate(product.ExpiryDate)}",
                    AlertSubTitle = $"{product.Name} (SKU: {product.SKU})",
                    ProductId = product.Id,
                    Type = NotificationType.ExpiryAlert,
                    Priority = isExpired ? nameof(AlertPriority.Critical) : nameof(AlertPriority.Warning),
                };
            }).OrderBy(p => p.ProductId);

            return alerts;
        }

        public async Task<IEnumerable<AlertDto>> ShowOutStockAlerts()
        {
            int branchId = 1;
            //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
            var alerts = await _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Where(x => x.BranchId == branchId
                && x.Quantity == 0)
                .OrderBy(x => x.ProductId)
                .Select(inventory => new AlertDto
                {
                    AlertTitle = "Out Of Stock Alert",
                    AlertDescription = $"{inventory.Product.Name} is completely out of stock",
                    AlertSubTitle = $"{inventory.Product.Name}(SKU: {inventory.Product.SKU})",
                    ProductId = inventory.ProductId,
                    Type = NotificationType.OutOfStock,
                    Priority = AlertPriority.Critical.ToString(),
                }).ToListAsync();

            return alerts;
        }

        public async Task<IEnumerable<AlertDto>> ShowOrderUpdates()
        {
            int branchId = 2;
            //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

            var recentDate = DateTime.UtcNow.AddDays(-2);

            var alerts = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId && 
                   (x.Status == OrderStatus.Pending || x.Status == OrderStatus.Deliverd && x.RecievedDeliveryDate >= recentDate))
            .OrderByDescending(a => a.OrderDate)
            .Select(order => new AlertDto
            {
            AlertTitle = order.Status == OrderStatus.Pending ? "Order Pending" : "Order Delivered",

            AlertDescription = order.Status == OrderStatus.Pending
                ? $"Order #{order.Id} is waiting for processing"
                : $"Order #{order.Id} has been successfully delivered",

            AlertSubTitle = order.Status == OrderStatus.Pending ? $"order created at: {order.OrderDate}" : $"order recived at: {order.RecievedDeliveryDate}",
            Type = NotificationType.OrderUpdate,
            Priority = AlertPriority.Info.ToString(),
            }).ToListAsync();
            return alerts;
        }

        //public async Task<object> Counts()
        //{
        //    var deadTask = ShowDeadStockAlerts().CountAsync();
        //    var outStockTask = ShowOutStockAlerts().CountAsync();
        //    var expiryTask = ShowExpiryAlerts().CountAsync();
        //    var lowStockTask = ShowLowStockAlerts().CountAsync();
        //    var infoTask = ShowOrderUpdates().CountAsync();

        //    await Task.WhenAll(deadTask, outStockTask, expiryTask, lowStockTask, infoTask);

        //    int deadCount = deadTask.Result;
        //    int outStockCount = outStockTask.Result;
        //    int expiryCount = expiryTask.Result;
        //    int lowStockCount = lowStockTask.Result;
        //    int infoCount = infoTask.Result;

        //    int criticalTotal = deadCount + outStockCount;
        //    int warningTotal = expiryCount + lowStockCount;

        //    return new
        //    {
        //        deadCount = deadCount,
        //        criticalCount = criticalTotal,
        //        warningCount = warningTotal,
        //        infoCount = infoCount
        //    };

        //}
        public async Task<AlertCountsDto> Counts()
        {
            var deadAlerts = await ShowDeadStockAlerts();
            int deadCount = deadAlerts.Count();

            var outOfStockAlerts = await ShowOutStockAlerts();
            int outCount = outOfStockAlerts.Count();

            var expiryAlerts = await ShowExpiryAlerts();
            int expiryCount = expiryAlerts.Count();

            var lowStockAlerts = await ShowLowStockAlerts();
            int lowCount = lowStockAlerts.Count();

            var infoAlerts = await ShowOrderUpdates();
            int infoCount = infoAlerts.Count();

            return new AlertCountsDto
            {
                DeadCount = deadCount,
                CriticalCount = deadCount + outCount,
                WarningCount = expiryCount + lowCount,
                InfoCount = infoCount
            };
        }
    }
}
