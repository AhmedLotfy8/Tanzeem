using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Dashboard;

namespace Tanzeem.Presentation.Dashboard
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController(IDashboardService _dashboardService) :ControllerBase
    {
        [HttpGet("get_four_boxes")]
        public async Task<IActionResult> GetFourBoxesAtTheTopOfPage()
        {
            var result = await _dashboardService.GetDashboardSummary();
            return Ok(result);
        }
        [HttpGet("get_top_moving_items")]
        public async Task<IActionResult> GetTopMovingItems()
        {
            var result = await _dashboardService.GetTopMovingItemsAsync();
            return Ok(result);
        }
        [HttpGet("get_category_distribution")]
        public async Task<IActionResult> GetCategoryDistribution()
        {
            var result = await _dashboardService.GetCategoryDistribution();
            return Ok(result);
        }
        [HttpGet("get_bar_chart_IN-OUT")]
        public async Task<IActionResult> GetBarChartInOutStock()
        {
            var result = await _dashboardService.GetMonthlyStockMovementAsync();
            return Ok(result);
        }
    }
}
