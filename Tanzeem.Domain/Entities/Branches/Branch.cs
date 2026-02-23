using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Transactions;

namespace Tanzeem.Domain.Entities.Branches {
    public class Branch {
    
        public int Id { get; set; }


        #region Relationships
        #endregion
        public int CompanyId { get; set; }  // fk


        #region Navigation
        #endregion
        public required Company Company { get; set; }
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        
    }
}


#region Later
//public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
//public ICollection<User> Users { get; set; } = new List<User>();
#endregion