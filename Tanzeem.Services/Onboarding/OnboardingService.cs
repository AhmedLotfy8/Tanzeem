using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Onboarding;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Onboarding;

namespace Tanzeem.Services.Onboarding {
    public class OnboardingService(IUnitOfWork unitOfWork) : IOnboardingService {
        

        public Task<string> OnboardNewTenantAsync(OnboardingDto onboardingDto) {
            throw new NotImplementedException();
        }
        private Task<int> AssignCompanyAndBranchToAdmin(int companyId, int branchId, int userId) {
            throw new NotImplementedException();
        }

    }
}

#region Note
//public async Task<int> CreateNewCompanyAsync(CompanyDto companyDto) {

//    #region Mapping
//    var company = new Company {
//        Name = companyDto.Name,
//        Field = companyDto.Field,
//        Email = companyDto.Email,
//        Phone = companyDto.Phone,
//        CreatedAt = DateTime.UtcNow,
//        IsActive = true
//    };
//    #endregion

//    await unitOfWork.GetRepository<Company>().AddAsync(company);
//    var count = await unitOfWork.SaveChangesAsync();

//    return company.Id;
//}
//public async Task<int> CreateDefaultBranchAsync(BranchDto branchDto) {

//    #region Mapping
//    var branch = new Branch {
//        Name = branchDto.Name,
//        Location = branchDto.Location,
//        PhoneNumber = branchDto.PhoneNumber,
//        Email = branchDto.Email,
//        CreatedAt = DateTime.UtcNow,
//        Status = BranchStatus.Active,
//        CompanyId = 3 // This should be the ID of the newly created company, but for now it's hardcoded to 1
//    };
//    #endregion

//    await unitOfWork.GetRepository<Branch>().AddAsync(branch);
//    var count = await unitOfWork.SaveChangesAsync();

//    return branch.Id;
//}

#endregion
