using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Notifications;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Alerts
{
    ///TODO filter by branchid after auth
    public class AlertService(IUnitOfWork _unitOfWork) : IAlertService
    {
        public async Task<PaginationResponseDto<AlertDto>> ShowAlerts(
        NotificationType? type, int page, int pageSize)
        {
            if (page <= 0) page = 1;
            if (pageSize > 20) pageSize = 20;

            switch (type)
            {
                case NotificationType.LowStockAlert:
                    return await ShowLowStockAlerts().OrderBy(x => x.Priority)
                                 .ToPaginatedResponseAsync(page, pageSize);

                case NotificationType.DeadStockAlert:
                    return await ShowDeadStockAlerts().OrderBy(x => x.Priority)
                                 .ToPaginatedResponseAsync(page, pageSize);

                case NotificationType.ExpiryAlert:
                    return await ShowExpiryAlerts().OrderBy(x => x.Priority)
                                 .ToPaginatedResponseAsync(page, pageSize);

                case NotificationType.OutOfStock:
                    return await ShowOutStockAlerts().OrderBy(x => x.Priority)
                                 .ToPaginatedResponseAsync(page, pageSize);

                default:
                    var all = ShowLowStockAlerts().AsEnumerable()
                        .Concat(ShowDeadStockAlerts().AsEnumerable())
                        .Concat(ShowExpiryAlerts().AsEnumerable())
                        .Concat(ShowOutStockAlerts().AsEnumerable())
                        .OrderBy(x => x.Priority);

                    return all.ToPaginatedResponse(page, pageSize);
            }
        }


        public IQueryable<AlertDto> ShowDeadStockAlerts()
        {

            var recentlySoldIds = _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.CreatedAt > DateTime.UtcNow.AddMonths(-3))
                .Select(ti => ti.ProductId)
                .Distinct();

            var rawQuery = _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Where(inv => inv.BranchId == 1 ///TODO Auth
                           && !recentlySoldIds.Contains(inv.ProductId))
                .Select(inv => new
                {
                    inv.ProductId,
                    inv.Product.Name,
                    inv.Product.SKU,
                    LastSaleDate = inv.Product.TransactionItems
                        .Where(ti => ti.Transaction.Type == TransactionType.Out)
                        .Select(ti => (DateTime?)ti.Transaction.CreatedAt)
                        .Max() 
                });

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
                });
            return alerts;
        }
        public IQueryable<AlertDto> ShowLowStockAlerts()
        {
            var alerts = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Include(x => x.Product)
                .Where(x => x.BranchId == 1 ///TODO auth
                && x.Quantity > 0
                && x.Quantity <= x.Product.ReorderLevel)
                .Select(inventory => new AlertDto
                {
                    AlertTitle = "Low Stock Alert",
                    AlertDescription = $"{inventory.Product.Name} stock is below minimum threshold",
                    AlertSubTitle = $"{inventory.Product.Name}(SKU: {inventory.Product.SKU}), Current Quantity: {inventory.Quantity}",
                    ProductId = inventory.ProductId,
                    Type = NotificationType.LowStockAlert,
                    Priority = AlertPriority.Warning.ToString(),
                });
            return alerts;         
        }
          
        public IQueryable<AlertDto> ShowExpiryAlerts()
        {
            var alerts = _unitOfWork.GetRepository<Product>().GetAllAsIQueryable()
                .Where(p => p.ExpiryDate <= DateTime.UtcNow.AddMonths(3))///TODO settings
                .Select(product => new AlertDto
                {
                    AlertTitle = "Expiry Warning",
                    AlertDescription = $"{product.Name} will expire in " +
                    $"{NotificationServiceHelper.GenerateSinceDate(product.ExpiryDate)}",
                    //AlertDescription = $"{product.Name} will expire in " + product.ExpiryDate,
                    AlertSubTitle = $"{product.Name} (SKU: {product.SKU})",
                    ProductId = product.Id,
                    Type = NotificationType.ExpiryAlert,
                    Priority = AlertPriority.Warning.ToString(),

                });   

            return alerts;
        }

        public IQueryable<AlertDto> ShowOutStockAlerts()
        {
            var alerts = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Include(x => x.Product)
                .Where(x => x.BranchId == 1
                && x.Quantity == 0) ///TODO auth
                .Select(inventory => new AlertDto
                {
                    AlertTitle = "Out Of Stock Alert",
                    AlertDescription = $"{inventory.Product.Name} is completely out of stock",
                    AlertSubTitle = $"{inventory.Product.Name}(SKU: {inventory.Product.SKU})",
                    ProductId = inventory.ProductId,
                    Type = NotificationType.OutOfStock,
                    Priority = AlertPriority.Critical.ToString(),
                });

            return alerts;
        }
    }
}
