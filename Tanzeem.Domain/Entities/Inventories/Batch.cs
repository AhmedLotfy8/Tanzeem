using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;
using Tanzeem.Domain.Entities.Branches;

namespace Tanzeem.Domain.Entities.Inventories {
    public class Batch : IAuditable {
    
        public int Id { get; set; }

        public string BatchId { get; set; }

        public int BatchQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }

        #region Relationships
        public int InventoryId { get; set; }
        public int BranchId { get; set; }
        
        public Inventory Inventory { get; set; } = default!;
        #endregion


        public byte[]? RowVersion { get; set; }   // concurrency token


    }
}
