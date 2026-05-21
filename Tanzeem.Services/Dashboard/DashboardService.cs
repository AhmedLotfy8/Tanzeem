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
using Tanzeem.Services.Abstractions.Dashboard;
using Tanzeem.Shared.Dtos.Dashboard;

namespace Tanzeem.Services.Dashboard
{
    public class DashboardService(IUnitOfWork _unitOfWork, IAlertService _alertService) : IDashboardService
    {
        private async Task<decimal> CalculateTotalStockValue()
        {
            var totalValue = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Where(inv => inv.Quantity > 0 && inv.BranchId == 1) ///TODO AUTH
                .SumAsync(inv => (inv.Quantity ?? 0) * inv.Product.CostPrice);

            return totalValue;
        }
        public async Task<DashboardBoxesDto> GetDashboardSummary()
        {
            return new DashboardBoxesDto()
            {
                LowStockCount = await _alertService.ShowLowStockAlerts().CountAsync(),
                DeadStockCount = await _alertService.ShowDeadStockAlerts().CountAsync(),
                NearExpiryCount = await _alertService.ShowExpiryAlerts().CountAsync(),
                TotalStockValue = await CalculateTotalStockValue()
            };
        }

        public async Task<List<TopMovingItemsDto>> GetTopMovingItemsAsync()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var sixtyDaysAgo = DateTime.UtcNow.AddDays(-60);

            var rawData = await _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.BranchId == 1 ///TODO: Auth
                          && ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.SourceReason == TransactionSource.Selling 
                          && ti.Transaction.CreatedAt >= sixtyDaysAgo) // بنسحب آخر شهرين بس
                .GroupBy(ti => new { ti.ProductId, ti.Product.Name })
                .Select(g => new
                {
                    ItemName = g.Key.Name,

                    CurrentUnits = g.Sum(ti => ti.Transaction.CreatedAt >= thirtyDaysAgo ? ti.QuantityOfTransactedItem : 0),
                    CurrentRevenue = g.Sum(ti => ti.Transaction.CreatedAt >= thirtyDaysAgo ? ti.QuantityOfTransactedItem * ti.UnitPrice : 0),

                    PreviousUnits = g.Sum(ti => ti.Transaction.CreatedAt < thirtyDaysAgo ? ti.QuantityOfTransactedItem : 0)
                })
                .Where(x => x.CurrentUnits > 0) 
                .OrderByDescending(x => x.CurrentUnits)
                .Take(10)
                .ToListAsync();

            
            var result = rawData.Select(x => new TopMovingItemsDto
            {
                ItemName = x.ItemName,
                UnitsSold = x.CurrentUnits,
                Revenue = Math.Round(x.CurrentRevenue, 2),
                Trend = x.CurrentUnits >= x.PreviousUnits ? "Rising" : "Falling"
            }).ToList();

            return result;
        }

        public async Task<List<CategoryDistributionDto>> GetCategoryDistribution()
        {
            var rawDistribution = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Where(inv => inv.BranchId == 1 && inv.Quantity > 0) ///TODO: Auth
                .GroupBy(inv => new { inv.Product.Category.Id, inv.Product.Category.Name })
                .Select(g => new
                {
                    CategoryName = g.Key.Name,
                    TotalQuantity = g.Sum(inv => inv.Quantity ?? 0)
                })
                .OrderByDescending(x => x.TotalQuantity)
                .ToListAsync();

            var result = new List<CategoryDistributionDto>();
            var top4Categories = rawDistribution.Take(4).ToList();

            foreach (var item in top4Categories)
            {
                result.Add(new CategoryDistributionDto
                {
                    CategoryName = item.CategoryName,
                    Count = item.TotalQuantity
                });
            }

            if (rawDistribution.Count > 4)
            {
                var othersCount = rawDistribution.Skip(4).Sum(x => x.TotalQuantity);
                result.Add(new CategoryDistributionDto
                {
                    CategoryName = "Others",
                    Count = othersCount
                });
            }

            return result;
        }

        public async Task<List<MonthlyMovementDto>> GetMonthlyStockMovementAsync()
        {
            var branchId = 1; ///TODO Auth

            var last12Months = Enumerable.Range(0, 12)
                .Select(i => DateTime.UtcNow.AddMonths(-11 + i))
                .ToList();

            var startDate = last12Months.First().Date; 

            var rawData = await _unitOfWork.GetRepository<Transaction>()
                .GetAllAsIQueryable()
                .Where(t => t.BranchId == branchId && t.CreatedAt >= startDate)
                .GroupBy(t => new {
                    Year = t.CreatedAt.Year,
                    Month = t.CreatedAt.Month
                })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    InCount = g.Sum(t => t.Type == TransactionType.In ? t.TotalTransactedItems : 0),
                    OutCount = g.Sum(t => t.Type == TransactionType.Out ? t.TotalTransactedItems : 0)
                })
                .ToListAsync();

            var result = last12Months.Select(m =>
            {
                var dbRecord = rawData.FirstOrDefault(r => r.Year == m.Year && r.Month == m.Month);

                return new MonthlyMovementDto
                {
                    MonthName = m.ToString("MMM"), 
                    StockIn = dbRecord != null ? dbRecord.InCount : 0,  
                    StockOut = dbRecord != null ? dbRecord.OutCount : 0 
                };
            }).ToList();

            return result;
        }
    }
}
