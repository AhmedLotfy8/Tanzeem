using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;

namespace Tanzeem.Domain.Entities.Transactions {
    public class Transaction {
    
        public int Id { get; set; }

        public string Type { get; set; }          // In_Out
    
        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }         // Pending_Completed_Failed


        #region Relationships
        #endregion
        public int BranchId { get; set; }


        #region Navigation
        #endregion
        public required Branch Branch { get; set; }
        public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();

    }
}
