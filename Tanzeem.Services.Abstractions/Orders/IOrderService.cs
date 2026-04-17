using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Orders;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Abstractions.Orders
{
    public interface IOrderService
    {
        Task<OrderResponseDto> GetOrderByIdAsync(int id);

       // Task<IEnumerable<OrderSummaryResponseDto>> GetAllOrdersAsync();

        Task<int> CreateOrderAsync(OrderRequestDto orderDto);


        Task<int> UpdateOrderAsync(int id, OrderRequestDto orderDto);

        Task<bool> DeleteOrderAsync(int id);

        public Task<IEnumerable<ProductLookupDto>> GetProductsLookupAsync(string searchTerm);

        public Task<PaginationResponseDto<OrderSummaryResponseDto>> GetOrdersWithPaginationAsync(int page, int pageSize);

        public Task<string> ChangeOrderToDeliverd(OrderConfirmDto confirmDto, int id);
        public IEnumerable<object> DisplayOrderStatuses();

        public int CountPendingOrders();
        public int CountDeliverdOrders();
    }
}
