using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Transactions {
    public class TransactionItem {
    
        public int Id { get; set; }

        public int Quantity { get; set; }

        public decimal UnitCost { get; set; }


        #region Relationships

        #endregion
        public int TransactionId { get; set; }
        public int ProductId { get; set; }


        #region Navigation

        #endregion
        public required Transaction Transaction { get; set; } = default!;
        public required Product Product { get; set; } = default!;

    }
}