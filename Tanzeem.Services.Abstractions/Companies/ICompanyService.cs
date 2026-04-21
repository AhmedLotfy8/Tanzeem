using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.Abstractions.Companies {
    public interface ICompanyService {
    
        Task<CompanyDto> GetCurrentCompanyAsync(int companyId); // Assuming you want to get a company by its ID

        Task<int> UpdateCompanyAsync(int companyId, CompanyDto companyDto);
        Task<int> CreateNewCompanyAsync(CompanyDto companyDto, int adminId);

        Task<bool> DeleteCompanyAsync(int companyId);



    }
}
