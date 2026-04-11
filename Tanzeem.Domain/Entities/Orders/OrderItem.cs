using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Orders
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Total { get; set; } //calculated

        #region Navigation Property
        #endregion
        public Product Product { get; set; }
        public Order Order { get; set; }

        #region Relations
        #endregion
        public int OrderId { get; set; }

    }
}
