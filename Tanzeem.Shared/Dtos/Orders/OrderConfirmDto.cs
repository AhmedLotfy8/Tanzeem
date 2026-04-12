using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Orders
{
    public class OrderConfirmDto
    {
        public DateTime? RecievedDate { get; set; }
        public IEnumerable<OrderItemsConfirmDto> ItemsConfirmDtos { get; set; } = new List<OrderItemsConfirmDto>();
    }
    public class OrderItemsConfirmDto
    {
        public int ProductId { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellPrice { get; set; }
        public int DamagedQuantity { get; set; }
    }
}
