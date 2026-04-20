using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.Companies {
    public class CompanyService(IUnitOfWork _unitOfWork) : ICompanyService {

        public async Task<CompanyDto> GetCurrentCompanyAsync(int companyId) { // Assuming companyId will be obtained from ClaimBasedTenant in the future, for now it's passed as a parameter

            var company = await _unitOfWork.GetRepository<Company>().GetByIdAsync(companyId);

            if (company is null)
                throw new Exception("company not found Exception");

            #region Mapping
            var result = new CompanyDto {
                Name = company.Name,
                Field = company.Field,
                Email = company.Email,
                Phone = company.Phone
            };
            #endregion

            return result;
        }

        public async Task<int> CreateNewCompanyAsync(CompanyDto companyDto, int adminId) {

            var admin = await _unitOfWork.GetRepository<User>().GetAsync(u => u.Id == adminId);

            #region Mapping
            var company = new Company {
                Name = companyDto.Name,
                Field = companyDto.Field,
                Email = companyDto.Email,
                Phone = companyDto.Phone,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
            };

            admin.Company = company;
            #endregion

            await _unitOfWork.GetRepository<Company>().AddAsync(company);

            var count = await _unitOfWork.SaveChangesAsync();

            return company.Id;
        }

        public async Task<int> UpdateCompanyAsync(int companyId, CompanyDto companyDto) { // Using companyId now, will use ClaimBasedTenant for implicied companyId in the future

            var company = _unitOfWork.GetRepository<Company>().GetByIdAsync(companyId).Result;

            if (company is null) {
                throw new Exception("Company not found");
            }

            #region Mapping
            company.Name = companyDto.Name;
            company.Field = companyDto.Field;
            company.Email = companyDto.Email;
            company.Phone = companyDto.Phone;
            #endregion


            await _unitOfWork.SaveChangesAsync();
            return company.Id;

        }

        public async Task<bool> DeleteCompanyAsync(int companyId) {

            var company = await _unitOfWork.GetRepository<Company>().GetByIdAsync(companyId);

            if (company is null) {
                throw new Exception("Company not found");
            }

            company.IsActive = false;
            var result = await _unitOfWork.SaveChangesAsync();
            
            return result > 0;

        }


    }
}

