using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Shared.Dtos.Transactions {
    public class TransactionItemDto {

        public int QuantityOfTransactedItem { get; set; }

        public decimal UnitPrice { get; set; }

        public ProductDto Product { get; set; } = default!;

    }
}
