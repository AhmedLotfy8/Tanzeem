using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Transactions {
    public class TransactionDto {

        public string Id { get; set; }

        public string Type { get; set; }          // In_Out_Adjustment

        public DateTime CreatedAt { get; set; }

        public string Status { get; set; }         // Pending_Completed_Failed

        public decimal Value { get; set; }

        public int Quantity { get; set; }

        public string SourceReason { get; set; }

        public string ReferenceNumber { get; set; }

        public string Notes { get; set; }

        public string PreformedBy { get; set; }

        public string BatchNumber { get; set; }



    }
}
