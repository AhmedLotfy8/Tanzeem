using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Presentation.BusinessCore {
    [ApiController]
    [Route("api/[controller]")]
    public class RegisterationController(IRegisterationService registerationService) 
        : ControllerBase {
        
        [HttpPost]
        [Route("CompanyCreation")]
        public async Task<IActionResult> CreateNewCompany(CompanyDto companyDto) {
            var result = await registerationService.CreateNewCompanyAsync(companyDto);
            return Ok(result);
        }

        [HttpPost]
        [Route("DefaultBranchCreation")]
        public async Task<IActionResult> CreateDefaultBranch(BranchDto branchDto) {
            var result = await registerationService.CreateDefaultBranchAsync(branchDto);
            return Ok(result);
        }


    }
}
