using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Notifications;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Users { 

    public class User {
    
        public int Id { get; set; }

        public string Name { get; set; }

        public string Email { get; set; }

        public string PasswordHash { get; set; }

        public UserRoles Role { get; set; }

        #region Relationships

        #endregion
        public int? CompanyId { get; set; }


        #region Navigation
        #endregion
        public Company Company { get; set; } = default!;
        public ICollection<BranchUserRelationship> BURelations { get; set; } = default!;



    }
}
