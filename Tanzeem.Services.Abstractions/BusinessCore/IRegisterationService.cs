using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Abstractions.BusinessCore {
    public interface IRegisterationService {

        Task<int> CreateNewCompanyAsync(CompanyDto companyDto);
        Task<int> CreateDefaultBranchAsync(BranchDto branchDto);

        //Task<int> AssignCompanyToUser(int companyId, int userId);




    }
}
