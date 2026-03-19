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

        public string Type { get; set; }          // In_Out_Adjustment

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }         // Pending_Completed_Failed

        public decimal Value { get; set; }

        public int Quantity { get; set; }

        public string SourceReason { get; set; }        

        public string ReferenceNumber { get; set; }

        public string Notes { get; set; }

        #region Later

        #endregion
        //public User PreformedBy { get; set; }
        // public String BatchNumber { get; set; }



        #region Relationships
        #endregion
        public int BranchId { get; set; }


        #region Navigation
        #endregion
        public required Branch Branch { get; set; }
        public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();

    }
}
