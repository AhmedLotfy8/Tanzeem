using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Orders
{
    public class Order
    {
        public int Id { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; } // calculated from order items
        public OrderStatus Status { get; set; } //enum
        public DateOnly ExpectedDeliveryDate { get; set; } 
        public DateOnly? RecievedDeliveryDate { get; set; } // can be null until recieving order
        public string? Notes { get; set; }

        #region Navigation property
        #endregion
        public Supplier Supplier { get; set; }
        public Company Company { get; set; }
        public Branch Branch { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        #region Relations
        #endregion
        public int SupplierId { get; set; }
        public int CompanyId { get; set; }
        public int BranchId { get; set; }


    }
}
