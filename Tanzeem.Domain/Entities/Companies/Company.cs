using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Entities.Users;

namespace Tanzeem.Domain.Entities.Companies {
    public class Company {
    
        public int Id { get; set; }

        public string Name { get; set; }       

        public string Field { get; set; }         

        public string Email { get; set; }

        public string Phone { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }


        #region Navigation
        #endregion
        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public ICollection<Product> Products  { get; set; } = new List<Product>();
        public ICollection<User> Users { get; set; } = new List<User>();
        
        public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();

    }
}