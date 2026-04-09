using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;

namespace Tanzeem.Domain.Entities.Users { 
     public enum UserRoles {
        Admin = 1,
        Manager = 2,
        Staff = 3,
    }

    public class User {
    
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public UserRoles Role { get; set; }

        #region Relationships

        #endregion
        public int CompanyId { get; set; }


        #region Navigation
        #endregion
        public Company Company { get; set; } = default!;

    }
}
