namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderItemResponseDto
    {
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; }
        public string ProductName { get; set; }
    }
}
