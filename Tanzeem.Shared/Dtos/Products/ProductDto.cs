using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Products {
    public class ProductDto {

        public string Name { get; set; }

        public string SKU { get; set; }

        public string? Category { get; set; }

        public int Stock { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SellingPrice { get; set; }

        public DateTime ExpiryDate { get; set; }

        public string Barcode { get; set; }

        public string Description { get; set; }
        public int ReorderLevel { get; set; }
        public string Status { get; set; }

    }
}
