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
using Tanzeem.Services.Abstractions.Alerts;
using Tanzeem.Services.Notifications;
using Tanzeem.Shared.Dtos.Notifications;

namespace Tanzeem.Services.Alerts
{
    public class AlertService(IUnitOfWork _unitOfWork) : IAlertService
    {       
        public IEnumerable<AlertDto> ShowAlerts(NotificationType? type)
        {
            List<AlertDto> alerts = new List<AlertDto>();

            switch (type)
            {
                case NotificationType.LowStockAlert:
                    alerts.AddRange(ShowLowStockAlerts());
                    break;
                case NotificationType.DeadStockAlert:
                    alerts.AddRange(ShowDeadStockAlerts());
                    break;
                default:
                    alerts.AddRange(ShowLowStockAlerts());
                    alerts.AddRange(ShowDeadStockAlerts());
                    break;
            }

            return alerts.OrderBy(x => Guid.NewGuid()).ToList();
        }

        public IEnumerable<AlertDto> ShowDeadStockAlerts()
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
                ///TODO auth
                .Where(inv => !recentlySoldIds.Contains(inv.ProductId) && inv.BranchId == 1)
                .ToList();
            if (!inventories.Any())
            {
                return Enumerable.Empty<AlertDto>();
            }
            var alerts = inventories.Select(inv =>
            {
                var allSalesDates = inv.Product.TransactionItems
                .Where(ti => ti.Transaction.Type == TransactionType.Out)
                .Select(ti => ti.Transaction.CreatedAt)
                .ToList();

                string dateResult;

                if (allSalesDates.Count == 0)
                {
                    dateResult = "No sales recorded yet";
                }
                else
                {
                    var lastDate = allSalesDates.Max();
                    dateResult = NotificationServiceHelper.GenerateSinceDate(lastDate);
                }
                return new AlertDto
                {
                    AlertTitle = "Dead Stock Alert",
                    AlertDescription = $"{inv.Product.Name} has not moved in {dateResult}",
                    AlertSubTitle = $"{inv.Product.Name}(SKU: {inv.Product.SKU})",
                    ProductId = inv.ProductId,
                    Type = NotificationType.DeadStockAlert,
                    Priority = AlertPriority.Critical.ToString(),
                };
            }).ToList();
            return alerts;

        }
        public IEnumerable<AlertDto> ShowLowStockAlerts()
        {
            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable()
                .Include(x => x.Product)
                .Where(x => x.BranchId == 1) ///TODO auth
                .ToList();

            List<AlertDto> lowAlerts = new List<AlertDto>();
            foreach (var inventory in inventories)
            {
                if (inventory.Quantity <= inventory.Product.ReorderLevel)
                {
                    AlertDto low = new AlertDto
                    {
                        AlertTitle = "Low Stock Alert",
                        AlertDescription = $"{inventory.Product.Name} stock is below minimum threshold",
                        AlertSubTitle = $"{inventory.Product.Name}(SKU: {inventory.Product.SKU}), Current Quantity: {inventory.Quantity}",
                        ProductId = inventory.ProductId,
                        Type = NotificationType.LowStockAlert,
                        Priority = AlertPriority.Warning.ToString(),
                    };
                    lowAlerts.Add(low);
                }
            }
            return lowAlerts;
        }
    }
}
