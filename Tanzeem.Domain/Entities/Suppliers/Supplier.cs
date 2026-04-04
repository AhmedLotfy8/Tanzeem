using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Suppliers
{
    public class Supplier
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PhoneNumberOne { get; set; }
        public string? PhoneNumberTwo { get; set; } //can be null
        public string? ContactPersonName { get; set; } //can be null
        public string? WebsiteURL { get; set; } //can be null
        public string Street { get; set; } 
        public string City { get; set; }
        public string Country { get; set; }
        public string? Tax_Id { get; set; }
        public string? Notes { get; set; }

        #region Navigation Property
        #endregion
        public ICollection<Order> Orders { get; set; } = new List<Order>();
        public Company Company { get; set; }

        #region Relations
        #endregion
        public int CompanyId { get; set; }
       
    }
}
