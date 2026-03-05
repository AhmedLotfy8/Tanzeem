using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Presentation.Companies {

    [ApiController]
    [Route("api/[controller]")]
    public class CompanyController(ICompanyService companyService)
        : ControllerBase {

        [HttpGet]
        [Route("Companies/{id}")]
        public async Task<IActionResult> GetCurrentCompany(int companyId) { // Assuming companyId will be obtained from ClaimBasedTenant in the future, for now it's passed as a parameter
            var result = await companyService.GetCurrentCompanyAsync(companyId);
            return Ok(result);
        }

        [HttpPut]
        [Route("Companies/{id}")]
        public async Task<IActionResult> UpdateCompany(int companyId, CompanyDto companyDto) {

            var result = await companyService.UpdateCompanyAsync(companyId, companyDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Companies/{id}")]
        public async Task<IActionResult> DeleteCompany(int companyId) {
            var result = await companyService.DeleteCompanyAsync(companyId);
            return Ok(result);
        }

    }
}
