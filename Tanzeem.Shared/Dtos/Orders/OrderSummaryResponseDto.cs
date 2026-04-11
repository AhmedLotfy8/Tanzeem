namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderSummaryResponseDto
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public string SupplierName { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
    }
}
