using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;

namespace Tanzeem.Domain.Entities.Transactions {

    #region Enums

    public enum TransactionType {
        In = 1,
        Out = 2,
        Adjustment = 3,
    }

    public enum TransactionStatus {
        Completed = 4,
        Pending = 5,
        Failed = 6,
    }

    public enum TransactionSource {
        Supplier = 7,
        Production = 8,
        Return = 9,
        Recovered = 10,
        FromAnotherBranch = 11,
        Adjustment = 12,
    }

    #endregion
    
    public class Transaction {

        public int Id { get; set; }

        public string TransactionId { get; set; }

        public TransactionType Type { get; set; }          // In_Out_Adjustment

        public DateTime CreatedAt { get; set; }

        public TransactionStatus Status { get; set; }         // Pending_Completed_Failed

        public decimal Value { get; set; }

        public int TotalTransactedItems { get; set; }

        public TransactionSource SourceReason { get; set; }    // Supplier_Return_Production_Recovered_FromAnoterBranch_Adjustment    

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
        public Branch Branch { get; set; } = default!;
        public ICollection<TransactionItem> TransactionItems { get; set; } = new List<TransactionItem>();

    }
}
