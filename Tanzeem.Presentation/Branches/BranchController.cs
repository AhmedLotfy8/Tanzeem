using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Shared.Dtos.Branches;

namespace Tanzeem.Presentation.Branches {

    [ApiController]
    [Route("api/[controller]")]
    public class BranchController(IBranchService branchService) :
        ControllerBase {


        [HttpGet]
        [Route("Branches/{id}")]
        public async Task<IActionResult> GetBranch(int id) {
            var branch = await branchService.GetBranchAsync(id);
            return Ok(branch);
        }

        [HttpPut]
        [Route("Branches/{id}")]
        public async Task<IActionResult> UpdateBranch(int id, BranchDto branchDto) {
            var result = await branchService.UpdateBranchAsync(id, branchDto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Branches/{id}")]
        public async Task<IActionResult> DeleteBranch(int id) {
            var result = await branchService.DeleteBranchAsync(id);
            return Ok(result);
        }

        [HttpGet]
        [Route("Branches")]
        public async Task<IActionResult> GetBranches(int id) {
            var branches = await branchService.GetCompanyBranchesAsync(id);
            return Ok(branches);
        }

        [HttpPost]
        [Route("Branches")]
        public async Task<IActionResult> CreateBranch(BranchDto branchDto) {
            var result = await branchService.CreateNewBranchAsync(branchDto);
            return Ok(result);
        }



        #region For later
        /*        public async Task<IActionResult> SetCurrentBranch(int id) {
                    var result = await branchService.SetCurrentBranchAsync(id);
                    return Ok(result);
                }
        */
        #endregion


    }
}
