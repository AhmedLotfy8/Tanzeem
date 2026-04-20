using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Branches {
    
    public class Branch {
    
        public int Id { get; set; }

        public string Name { get; set; } = default!;

        public string? Location { get; set; }

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }

        public BranchStatus Status { get; set; } = BranchStatus.Active;


        #region Relationships
        #endregion
        public int CompanyId { get; set; }  // fk


        #region Navigation
        #endregion
        public Company Company { get; set; } = default!;
        public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        public ICollection<BranchUserRelationship> BURelations { get; set; }

        public ICollection<Order> Orders { get; set; } = new List<Order>();


    }
}


#region Later
//public ICollection<User> Users { get; set; } = new List<User>();
#endregion