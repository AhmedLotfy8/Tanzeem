using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.Abstractions.BusinessCore {
    public interface IRegisterationService {

        Task<int> CreateNewCompany(CompanyDto companyDto);

        //Task<int> CreateNewAdmin(); // User dto to be created later

        //Task<int> AssignCompanyToUser(int companyId, int userId);

        //Task<int> CreateDefaultBranch();



    }
}
