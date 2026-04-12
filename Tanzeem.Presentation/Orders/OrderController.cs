using Microsoft.AspNetCore.Mvc;
using Tanzeem.Services.Abstractions.Orders;
using Tanzeem.Shared.Dtos.Orders;

namespace Tanzeem.Presentation.Orders
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController(IOrderService _orderService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> CreateOrder(OrderRequestDto orderDto)
        {
            var result = await _orderService.CreateOrderAsync(orderDto);
            return Ok(result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var result = await _orderService.DeleteOrderAsync(id);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> ViewOrders()
        {
            var result = await _orderService.GetAllOrdersAsync();
            return Ok(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateOrderDetails(int id, OrderRequestDto orderRequestDto)
        {
            var result = await _orderService.UpdateOrderAsync(id, orderRequestDto);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> ViewOrderDetails(int id)
        {
            var result = await _orderService.GetOrderByIdAsync(id);
            return Ok(result);
        }

        [HttpGet("Pagination")]
        public async Task<IActionResult> ViewOrdersWithPagination([FromQuery(Name = "Page_Size")] int pageSize, [FromQuery(Name = "Page")] int page = 1)
        {
            var result = await _orderService.GetOrdersWithPaginationAsync(page,pageSize);
            return Ok(result);
        }

        [HttpPut("ConfirmDelivery/{id}")]
        public async Task<IActionResult> ConfirmDelivery(int id, OrderConfirmDto confirmDto)
        {
            var result = await _orderService.ChangeOrderToDeliverd(confirmDto, id);
            return Ok(result);
        }

        [HttpGet("Lookup_Products")]
        public async Task<IActionResult> GetProductsLookup(string term)
        {
            var result = await _orderService.GetProductsLookupAsync(term);
            return Ok(result);
        }

        [HttpGet("display_order_status")]
        public IActionResult DisplayOrderStatuses()
        {
            var result = _orderService.DisplayOrderStatuses();
            return Ok(result);
        }

        [HttpGet("Pending_Order_Count")]
        public IActionResult CountPendingOrders()
        {
            var result = _orderService.CountPendingOrders();
            return Ok(result);
        }
        
        [HttpGet("Delivered_Order_Count")]
        public IActionResult CountDeliverdOrders()
        {
            var result = _orderService.CountDeliverdOrders();
            return Ok(result);
        }
    }
}
