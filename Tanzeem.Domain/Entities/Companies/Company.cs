using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Domain.Entities.Companies {
    public class Company {
    
        public int Id { get; set; }

        public string Field { get; set; }         // Agriculture, Manufacturing, Pharmaceutical, etc.



        #region Navigation
        #endregion
        public ICollection<Branch> Branches { get; set; } = new List<Branch>();
        public ICollection<Product> Products  { get; set; } = new List<Product>();

    }
}


#region Later
//public ICollection<Supplier> Suppliers { get; set; } = new List<Supplier>();
//public ICollection<User> Users { get; set; } = new List<User>();
#endregion