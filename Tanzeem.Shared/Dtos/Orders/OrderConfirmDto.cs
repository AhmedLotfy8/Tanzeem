
namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderConfirmDto
    {
        public int OrderId { get; set; }
        public DateTime? RecievedDate { get; set; }
        public IEnumerable<OrderItemsConfirmDto> ItemsConfirmDtos { get; set; } = new List<OrderItemsConfirmDto>();
        public string? Notes { get; set; }
    }
    public class OrderItemsConfirmDto
    {
        public int ProductId { get; set; }
        //unit and cost price edit
        public int? DamagedQuantity { get; set; }
        public int? DefectiveQuantity { get; set; }
        public int? MissingQuantity { get; set; }
        public int? IncorrectQuantity { get; set; }
    }
    public class OrderConfirmResponseDto
    {
        public int OrderId { get; set; }
        public string SupplierName { get; set; }
        public IEnumerable<OrderItemConfirmResponseDto> ItemsConfirmResponseDtos { get; set; } = new List<OrderItemConfirmResponseDto>();
    }

    public class OrderItemConfirmResponseDto
    {
    public int ProductId { get; set; }
    public int OrderedQuantity { get; set; }
    public string SKU { get; set; }
    public decimal Price { get; set; }
    }
}
