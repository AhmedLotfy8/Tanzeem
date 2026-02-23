using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Domain.Entities.Products {
    public class Product {
        
        public int Id { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }


        #region Relationships

        #endregion
        public int CompanyId { get; set; }


        #region Navigation
        #endregion
        public required Company Company { get; set; }
        public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();


    }
}
