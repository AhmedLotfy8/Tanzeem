using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.AI;

namespace Tanzeem.Presentation.AI
{
    [ApiController]
    [Route("api/[controller]")]
    public class DemandForecastingController (IDemandForecastingService _demandForecastingService): ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetPredictions([FromQuery(Name = "page_size")] int pageSize, [FromQuery(Name = "page")] int page = 1)
        {
            var result = await _demandForecastingService.GetAllPredictionsAsync(pageSize, page);
            return Ok(result);
        }
    }
}
