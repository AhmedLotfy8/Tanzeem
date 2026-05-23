using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.AI;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.DemandForecast;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;


public class DemandForecastingService(IUnitOfWork _unitOfWork, HttpClient _httpClient,
    ICurrentService _currentService) : IDemandForecastingService
{       
    
    public async Task<PaginationResponseDto<AIDemandForecastResponseDto>> GetAllPredictionsAsync(int page, int pageSize)
    {
        int branchId = 1; ///TODO auth 
        
        //int branchId = _currentService.BranchId ?? throw new UnauthorizedAccessException("User is not assigned to any branch.");

        if (page <= 0) page = 1;

        const int maxPageSize = 20;

        if (pageSize > maxPageSize) pageSize = maxPageSize;

        var predictions = _unitOfWork.GetRepository<DemandForecast>().GetAllAsIQueryable()
            .Where(x => x.BranchId == branchId);

        var totalCount = await predictions.CountAsync();

        var mappedData = await predictions.Select(p => new AIDemandForecastResponseDto
        {
            ProductId = p.ProductId,
            ProductName = p.Product.Name,
            SKU = p.Product.SKU,
            DemandOccurs = p.DemandOccurs,
            PredictedUnits = (double)p.PredictedUnits,
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
    
    /// <summary>
    /// كلاس مساعد صغير عشان ننقل بيه داتا المبيعات بين الميثودس بدل الـ Anonymous Type
    /// </summary>
    private class ProductSaleData
    {
        public int ProductId { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>
    /// 1. المُنظِّم الرئيسي (Orchestrator) - يجمع البيانات ويدير العملية
    /// </summary>
    public async Task UpdateAllForecastsAsync()
    {
        // 1. هنجيب كل الـ IDs بتاعة الفروع اللي في السيستم
        var allBranchIds = await _unitOfWork.GetRepository<Branch>()
            .GetAllAsIQueryable()
            .Select(b => b.Id)
            .ToListAsync();

        var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30).Date;

        // 2. هنلف على فرع فرع نحدث التوقعات بتاعته
        foreach (var branchId in allBranchIds)
        {
            // أ. سحب داتا المبيعات والمخزون الحالية (للفـرع ده)
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

            var inventories = await _unitOfWork.GetRepository<Inventory>()
                .GetAllAsIQueryable()
                .Include(i => i.Product)
                .Where(i => i.BranchId == branchId)
                .ToListAsync();

            double overallStoreAvg = rawSales.Any() ? rawSales.Average(x => x.Quantity) : 0;

            // ب. بناء الـ JSON للـ AI 
            var requestBatch = BuildFlaskRequests(inventories, rawSales, branchId, overallStoreAvg);

            var existingForecasts = await _unitOfWork.GetRepository<DemandForecast>()
                .GetAllAsIQueryable()
                .Where(f => f.BranchId == branchId)
                .ToDictionaryAsync(f => f.ProductId);

            // جـ. نكلم الـ API
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

        // هـ. حفظ كل التغييرات في قاعدة البيانات دفعة واحدة بعد ما نخلص كل الفروع
        await _unitOfWork.SaveChangesAsync();
    }

    /// <summary>
    /// 2. المسؤول عن الـ HTTP (Network Responsibility)
    /// </summary>
    private async Task<AIDemandForecastResponseDto?> CallFlaskApiAsync(AIDemandForecastRequestDto requestItem)
    {
        try
        {
            string apiUrl = "https://yasminesherbeny-forecast-api.hf.space/api/predict-from-raw";

            var response = await _httpClient.PostAsJsonAsync(apiUrl, requestItem);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AIDemandForecastResponseDto>();
            }
            else
            {
                // لو الـ API رفض الطلب، هيقولنا السبب هنا
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"🚨 API Error: {response.StatusCode} - {errorContent}");
                return null;
            }
        }
        catch (Exception ex)
        {
            // لو فيه مشكلة في الشبكة أو اللينك غلط، هتطبع هنا
            Console.WriteLine($"🚨 Exception calling AI API: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// 3. المسؤول عن لوجيك الداتابيز (Update or Create)
    /// </summary>
    private async Task ApplyUpsertToDatabase(int productId, int branchId, AIDemandForecastResponseDto prediction, Dictionary<int, DemandForecast> existingForecasts)
    {
        // تاريخ التوقع (بما إن الـ AI بيتوقع لبكرة، هنزود يوم على تاريخ النهاردة)
        var targetForecastDate = DateTime.UtcNow.AddDays(1).Date;

        // لو المنتج ليه توقع قديم متسجل -> نعمله Update
        if (existingForecasts.TryGetValue(productId, out var existingForecast))
        {
            existingForecast.PredictedUnits = (decimal)prediction.PredictedUnits;
            existingForecast.DemandOccurs = prediction.DemandOccurs;
            existingForecast.Segment = prediction.Segment;
            existingForecast.Confidence = (decimal)prediction.Confidence;
            existingForecast.ForecastDate = targetForecastDate;
            existingForecast.LastUpdated = DateTime.UtcNow;
        }
        // لو منتج جديد ملوش توقع متسجل -> نعمله Create
        else
        {
            var newForecast = new DemandForecast
            {
                ProductId = productId,
                BranchId = branchId,
                PredictedUnits = (decimal)prediction.PredictedUnits,
                DemandOccurs = prediction.DemandOccurs,
                Segment = prediction.Segment,
                Confidence = (decimal)prediction.Confidence,
                ForecastDate = targetForecastDate,
                LastUpdated = DateTime.UtcNow
            };
            await _unitOfWork.GetRepository<DemandForecast>().AddAsync(newForecast);
        }
    }

    /// <summary>
    /// 4. ميثود مساعدة لبناء الـ Request List
    /// </summary>
    private List<AIDemandForecastRequestDto> BuildFlaskRequests(List<Inventory> inventories, List<ProductSaleData> rawSales, int branchId, double overallStoreAvg)
    {
        var batch = new List<AIDemandForecastRequestDto>();

        // سحبنا داتا المبيعات في Memory Dictionary عشان الـ Performance يبقى "طلقة" زي ما اتفقنا
        var salesByProduct = rawSales.ToLookup(x => x.ProductId);

        // 1. تحديد تاريخ التوقع (بكرة)
        var targetForecastDate = DateTime.UtcNow.AddDays(1);

        // 2. تطبيق لوجيك الـ Holiday زي ما في الورقة بالظبط
        var dayOfWeek = targetForecastDate.DayOfWeek;
        int isHoliday = (dayOfWeek == DayOfWeek.Thursday || dayOfWeek == DayOfWeek.Friday || dayOfWeek == DayOfWeek.Saturday) ? 1 : 0;

        // تاريخ النهارده (عشان نحسب بيه الـ units ordered)
        var todayDate = DateTime.UtcNow.Date;

        foreach (var inv in inventories)
        {
            // سحب مبيعات المنتج ده بس من غير لود على الداتابيز
            var productSales = salesByProduct[inv.ProductId].ToList();

            List<DailyHistoryDto> history = new();
            List<int> dailyUnits = new();

            for (int i = 30; i >= 1; i--)
            {
                var historyDate = DateTime.UtcNow.AddDays(-i).Date;
                var unitsSold = productSales.Where(x => x.Date == historyDate).Sum(x => x.Quantity);

                history.Add(new DailyHistoryDto { Date = historyDate.ToString("yyyy-MM-dd"), UnitsSold = unitsSold });
                dailyUnits.Add(unitsSold);
            }

            // 3. تطبيق لوجيك الـ units_ordered (مبيعات النهارده للمنتج ده)
            var todayUnitsOrdered = productSales.Where(x => x.Date == todayDate).Sum(x => x.Quantity);

            batch.Add(new AIDemandForecastRequestDto
            {
                BranchId = $"STORE_{branchId:D3}",
                ProductId = $"P_{inv.ProductId:D3}",

                // تاريخ التوقع (بكرة)
                Date = targetForecastDate.ToString("yyyy-MM-dd"),

                Price = inv.Product.SellingPrice,

                Discount = 0, // زي الورقة

                HolidayPromotion = isHoliday, // 👈 اتعدلت حسب أيام الأسبوع (الخميس، الجمعة، السبت)

                InventoryLevel = inv.Quantity ?? 0,

                UnitsOrdered = todayUnitsOrdered, // 👈 اتعدلت حسب مجموع منصرف اليوم

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
        if (!values.Any()) return 0;
        double avg = values.Average();
        double sum = values.Sum(d => Math.Pow(d - avg, 2));
        return Math.Sqrt(sum / values.Count());
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