using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Transactions {
    public class TransactionItem {
    
        public int Id { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }


        #region Relationships

        #endregion
        public int TransactionId { get; set; }
        public int ProductId { get; set; }


        public Transaction Transaction { get; set; } = default!;
        public Product Product { get; set; } = default!;



    }
}