using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Services.Abstractions.Companies {
    public interface ICompanyService {
    
        Task<CompanyDto> GetCurrentCompanyAsync();
        
        Task<int> UpdateCompanyAsync(int companyId, CompanyDto companyDto);

        Task<bool> DeleteCompanyAsync(int companyId);


    }
}
