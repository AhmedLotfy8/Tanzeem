using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Settings;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.AI;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.DemandForecast;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

public class DemandForecastingService(IUnitOfWork _unitOfWork, HttpClient _httpClient,
    ICurrentService _currentService, IConfiguration _configuration) : IDemandForecastingService
{
    public async Task<PaginationResponseDto<AIDemandForecastResponseDto>> GetAllPredictionsAsync(int page, int pageSize)
    {
        //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
        
        int branchId = 1;

        if (page <= 0) page = 1;

        const int maxPageSize = 20;

        if (pageSize > maxPageSize) pageSize = maxPageSize;

        var predictions = _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId);

        var totalCount = await predictions.CountAsync();

        var mappedData = await predictions
        .OrderByDescending(x => x.PredictedUnits)
        .ThenBy(x => x.Id)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(p => new AIDemandForecastResponseDto
        {
        ProductId = p.ProductId,
        ProductName = p.Product.Name,
        SKU = p.Product.SKU,
        DemandOccurs = p.DemandOccurs,
        PredictedUnits = (int)p.PredictedUnits,
        Segment = p.Segment,
        Confidence = (double)p.Confidence,
        }).ToListAsync();

        return new PaginationResponseDto<AIDemandForecastResponseDto>
        {
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Data = mappedData
        };
    }

    public async Task<IEnumerable<TopCategoriesByForecastDto>> GetTopCategoriesByForecast()
    {
        int branchId = 1;
        //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 

        var topCategories = await _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId)
            .GroupBy(x => new { CategoryId = x.Product.CategoryId, CategoryName = x.Product.Category.Name })
            .Select(g => new TopCategoriesByForecastDto
            {
                CategoryId = g.Key.CategoryId,
                CategoryName = g.Key.CategoryName ?? "Uncategorized",
                CategoryCount = (int)g.Sum(x => x.PredictedUnits)
            })
            .OrderByDescending(c => c.CategoryCount)
            .Take(10)
            .ToListAsync();

        return topCategories;
    }

    public async Task<DemandDashboardDto> GetCounts()
    {
        //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("No branch id assigned"); 
        
        int branchId = 1;
        var demandItems = _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId);

        var TotalProductForecasted = await demandItems.CountAsync();

        var HighDemandItems = await demandItems.Where( x => x.Segment.ToLower() == "high").CountAsync();

        var averageConfidence = await demandItems.AverageAsync(f => (double?)f.Confidence) ?? 0;
        var confidencePercentage = Math.Round(averageConfidence * 100);

        var itemsNeedRestock = await demandItems.Join(_unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable(),
          forecast => new { forecast.ProductId, forecast.BranchId },
          inventory => new { inventory.ProductId, inventory.BranchId },
          (forecast, inventory) => new { forecast, inventory })
            .Where(x => x.inventory.Quantity <= x.forecast.PredictedUnits)
            .CountAsync();
        return new DemandDashboardDto
        {
            TotalProductForecasted = TotalProductForecasted,
            HighDemandItems = HighDemandItems,
            AverageForecastConfidence = confidencePercentage,
            ItemsNeedRestock = itemsNeedRestock
        };
    }

    private class ProductSaleData
    {
        public int ProductId { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }

    public async Task UpdateAllForecastsAsync()
    {
        var settings = await _unitOfWork.GetRepository<AIConfigurations>().GetAllAsIQueryable()
        .ToDictionaryAsync(s => s.BranchId, s => s.DemandForecasting);

        var allBranchIds = await _unitOfWork.GetRepository<Branch>()
        .GetAllAsIQueryable()
        .Select(b => b.Id)
        .ToListAsync();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
        var todayDate = DateTime.UtcNow.Date;

        foreach (var branchId in allBranchIds)
        {
            bool isForecastingEnabled = settings.TryGetValue(branchId, out bool isEnabled) && isEnabled;
            
            if(!isForecastingEnabled)
            {
                continue;
            }
            var rawSales = await _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsIQueryable()
                .Where(ti => ti.Transaction.BranchId == branchId
                          && ti.Transaction.Type == TransactionType.Out
                          && ti.Transaction.CreatedAt >= thirtyDaysAgo)
                .Select(ti => new ProductSaleData
                {
                    ProductId = ti.ProductId,
                    Date = ti.Transaction.CreatedAt.Date,
                    Quantity = ti.QuantityOfTransactedItem
                })
                .ToListAsync();

            // 2. سحب كميات الأوردرات الخاصة بـ "اليوم فقط" وتجميعها في Dictionary
            var todayOrdersRaw = await _unitOfWork.GetRepository<OrderItem>()
                .GetAllAsIQueryable()
                .Where(oi => oi.Order.BranchId == branchId && oi.Order.OrderDate.Date == todayDate)
                .Select(oi => new
                {
                    ProductId = oi.ProductId,
                    Quantity = oi.Quantity
                })
                .ToListAsync();

            Dictionary<int, int> ordersTodayByProduct = todayOrdersRaw
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

            // 3. سحب المخزون
            var inventories = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId)
                .ToListAsync();

            double overallStoreAvg = rawSales.Any() ? rawSales.Average(x => x.Quantity) : 0;

            // 4. بناء الـ Payload للموديل وتمرير الـ 30 يوم
            var requestBatch = BuildFlaskRequests(inventories, rawSales, ordersTodayByProduct, branchId, overallStoreAvg, thirtyDaysAgo);

            var existingForecasts = await _unitOfWork.GetRepository<DemandForecast>()
                .GetAllAsIQueryable()
                .Where(f => f.BranchId == branchId)
                .ToDictionaryAsync(f => f.ProductId);

            // 5. الاتصال بالـ AI API وحفظ النتيجة
            foreach (var requestItem in requestBatch)
            {
                var aiPrediction = await CallFlaskApiAsync(requestItem);

                if (aiPrediction != null)
                {
                    int actualProductId = int.Parse(requestItem.ProductId.Replace("P_", ""));
                    await ApplyUpsertToDatabase(actualProductId, branchId, aiPrediction, existingForecasts);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task UpdateForecastForBranchAsync(int branchId)
    {
        // 1. نتأكد إن الخاصية متفعلة للفرع ده (أمان إضافي لو اتنادت من أي مكان تاني)
        var aiConfig = await _unitOfWork.GetRepository<AIConfigurations>()
            .GetAllAsIQueryable()
            .FirstOrDefaultAsync(c => c.BranchId == branchId);

        if (aiConfig == null || !aiConfig.DemandForecasting)
        {
            return; // لو الإعدادات مش موجودة أو مقفولة، اقفل التاسك فوراً
        }

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;
        var todayDate = DateTime.UtcNow.Date;

        // 2. سحب المبيعات لآخر 30 يوم للفرع ده بس
        var rawSales = await _unitOfWork.GetRepository<TransactionItem>()
            .GetAllAsIQueryable()
            .Where(ti => ti.Transaction.BranchId == branchId
                      && ti.Transaction.Type == TransactionType.Out
                      && ti.Transaction.CreatedAt >= thirtyDaysAgo)
            .Select(ti => new ProductSaleData
            {
                ProductId = ti.ProductId,
                Date = ti.Transaction.CreatedAt.Date,
                Quantity = ti.QuantityOfTransactedItem
            })
            .ToListAsync();

        // 3. سحب كميات الأوردرات الخاصة بـ "اليوم فقط" للفرع ده وتجميعها
        var todayOrdersRaw = await _unitOfWork.GetRepository<OrderItem>()
            .GetAllAsIQueryable()
            .Where(oi => oi.Order.BranchId == branchId && oi.Order.OrderDate.Date == todayDate)
            .Select(oi => new
            {
                ProductId = oi.ProductId,
                Quantity = oi.Quantity
            })
            .ToListAsync();

        Dictionary<int, int> ordersTodayByProduct = todayOrdersRaw
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));

        // 4. سحب المخزون الخاص بالفرع ده
        var inventories = await _unitOfWork.GetRepository<Inventory>()
            .GetAllAsIQueryable()
            .Include(i => i.Product)
            .Where(i => i.BranchId == branchId)
            .ToListAsync();

        double overallStoreAvg = rawSales.Any() ? rawSales.Average(x => x.Quantity) : 0;

        // 5. بناء الـ Payload للموديل
        var requestBatch = BuildFlaskRequests(inventories, rawSales, ordersTodayByProduct, branchId, overallStoreAvg, thirtyDaysAgo);

        // 6. سحب التوقعات القديمة للفرع ده عشان نعملها Update
        var existingForecasts = await _unitOfWork.GetRepository<DemandForecast>()
            .GetAllAsIQueryable()
            .Where(f => f.BranchId == branchId)
            .ToDictionaryAsync(f => f.ProductId);

        // 7. الاتصال بالـ AI API وحفظ النتيجة
        foreach (var requestItem in requestBatch)
        {
            var aiPrediction = await CallFlaskApiAsync(requestItem);

            if (aiPrediction != null)
            {
                int actualProductId = int.Parse(requestItem.ProductId.Replace("P_", ""));
                await ApplyUpsertToDatabase(actualProductId, branchId, aiPrediction, existingForecasts);
            }
        }

        // 8. حفظ التغييرات للفرع ده في خبطة واحدة
        await _unitOfWork.SaveChangesAsync();
    }

    private async Task<AIDemandForecastResponseDto?> CallFlaskApiAsync(AIDemandForecastRequestDto requestItem)
    {
        try
        {
            string apiUrl = _configuration["AIModels:ForecastApiUrl"] ?? throw new InvalidOperationException("API URL is missing in appsettings.json!");

            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestItem);

            if (response.IsSuccessStatusCode)
            {
                // 1. هنقرأ الداتا كنص الأول قبل ما C# يلمسها
                string rawJson = await response.Content.ReadAsStringAsync();

                // 2. هنطبع شكل الداتا الحقيقي اللي البايثون بعته
                Console.WriteLine($"🔍 RAW JSON FROM PYTHON: {rawJson}");

                try
                {
                    // 3. هنحاول نحولها
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return System.Text.Json.JsonSerializer.Deserialize<AIDemandForecastResponseDto>(rawJson, options);
                }
                catch (System.Text.Json.JsonException ex)
                {
                    // 4. لو ضربت، هنطبع السبب الحرفي اللي مزعل C#
                    Console.WriteLine($"🚨 EXACT JSON ERROR: {ex.Message}");
                    return null;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🚨 API Error: {response.StatusCode} - {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🚨 Exception calling AI API: {ex.Message}");
            return null;
        }
    }

    private async Task ApplyUpsertToDatabase(int productId, int branchId, AIDemandForecastResponseDto prediction, Dictionary<int, DemandForecast> existingForecasts)
    {
        var targetForecastDate = DateTime.UtcNow.AddDays(1).Date;

        if (existingForecasts.TryGetValue(productId, out var existingForecast))
        {
            existingForecast.PredictedUnits = (int)Math.Round((decimal)prediction.PredictedUnits, MidpointRounding.AwayFromZero);
            existingForecast.DemandOccurs = prediction.DemandOccurs;
            existingForecast.Segment = prediction.Segment;
            existingForecast.Confidence = (decimal)prediction.Confidence;
            existingForecast.ForecastDate = targetForecastDate;
            existingForecast.LastUpdated = DateTime.UtcNow;
            _unitOfWork.GetRepository<DemandForecast>().UpdateAsync(existingForecast);
        }
        else
        {
            var newForecast = new DemandForecast
            {
                ProductId = productId,
                BranchId = branchId,
                PredictedUnits = (int)Math.Round((decimal)prediction.PredictedUnits, MidpointRounding.AwayFromZero),
                DemandOccurs = prediction.DemandOccurs,
                Segment = prediction.Segment,
                Confidence = (decimal)prediction.Confidence,
                ForecastDate = targetForecastDate,
                LastUpdated = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<DemandForecast>().AddAsync(newForecast);
        }
    }

    private List<AIDemandForecastRequestDto> BuildFlaskRequests(List<Inventory> inventories, List<ProductSaleData> rawSales, Dictionary<int, int> ordersTodayByProduct, int branchId, double overallStoreAvg, DateTime thirtyDaysAgo)
    {
        var batch = new List<AIDemandForecastRequestDto>();

        var salesByProduct = rawSales.ToLookup(x => x.ProductId);
        var targetForecastDate = DateTime.UtcNow.AddDays(1);
        var dayOfWeek = targetForecastDate.DayOfWeek;
        int isHoliday = (dayOfWeek == DayOfWeek.Thursday || dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday) ? 1 : 0;

        // حساب 30 يوم من المتغير الجديد
        int historyDays = (DateTime.UtcNow.Date - thirtyDaysAgo).Days;

        foreach (var inv in inventories)
        {
            var productSales = salesByProduct[inv.ProductId].ToList();
            List<DailyHistoryDto> history = new();
            List<int> dailyUnits = new();

            for (int i = historyDays; i >= 1; i--)
            {
                var historyDate = DateTime.UtcNow.AddDays(-i).Date;
                var unitsSold = productSales.Where(x => x.Date == historyDate).Sum(x => x.Quantity);

                history.Add(new DailyHistoryDto { Date = historyDate.ToString("yyyy-MM-dd"), UnitsSold = unitsSold });
                dailyUnits.Add(unitsSold);
            }

            var todayUnitsOrdered = ordersTodayByProduct.TryGetValue(inv.ProductId, out int quantity) ? quantity : 0;

            batch.Add(new AIDemandForecastRequestDto
            {
                BranchId = $"STORE_{branchId:D3}",
                ProductId = $"P_{inv.ProductId:D3}",
                Date = targetForecastDate.ToString("yyyy-MM-dd"),
                Price = inv.Product.SellingPrice,
                Discount = 0,
                HolidayPromotion = isHoliday,
                InventoryLevel = inv.Quantity ?? 0,
                UnitsOrdered = todayUnitsOrdered,
                History = history,
                ProductStats = new ProductStatsDto
                {
                    Mean = Math.Round(dailyUnits.Average(), 2),
                    Max = dailyUnits.Max(),
                    Min = dailyUnits.Min(),
                    Std = Math.Round(CalculateStdDev(dailyUnits), 2),
                    Median = CalculateMedian(dailyUnits)
                },
                StoreAvg = Math.Round(overallStoreAvg, 2)
            });
        }
        return batch;
    }

    #region Helpers
    private double CalculateStdDev(IEnumerable<int> values)
    {
        int count = values.Count();
        if (count <= 1) return 0;

        double avg = values.Average();
        double sum = values.Sum(d => Math.Pow(d - avg, 2));
        return Math.Sqrt(sum / (count - 1));
    }

    private double CalculateMedian(IEnumerable<int> values)
    {
        var sorted = values.OrderBy(n => n).ToArray();
        if (sorted.Length == 0) return 0;
        int mid = sorted.Length / 2;
        return (sorted.Length % 2 != 0) ? sorted[mid] : (sorted[mid - 1] + sorted[mid]) / 2.0;
    }
    #endregion
}