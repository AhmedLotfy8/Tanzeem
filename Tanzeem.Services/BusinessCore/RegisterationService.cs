using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.BusinessCore {
    public class RegisterationService(IUnitOfWork _unitOfWork)
        : IRegisterationService {
        

        public async Task<int> CreateNewCompany(CompanyDto companyDto) {

            #region Mapping
            var company = new Company {
                Name = companyDto.Name,
                Field = companyDto.Field,
                Email = companyDto.Email,
                Phone = companyDto.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            #endregion

            var result = _unitOfWork.GetRepository<Company>().AddAsync(company);
            var count = await _unitOfWork.SaveChangesAsync();

            return company.Id;
        }

        #region To Be Implemented 
        //public Task<int> AssignCompanyToUser(int companyId, int userId) {
        //    throw new NotImplementedException();
        //}

        //public Task<int> CreateDefaultBranch() {
        //    throw new NotImplementedException();
        //}

        //public Task<int> CreateNewAdmin() {
        //    throw new NotImplementedException();
        //}
        #endregion

    }
}
