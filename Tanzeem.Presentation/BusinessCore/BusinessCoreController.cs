using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Presentation.BusinessCore {

    [ApiController]
    [Route("api/[controller]")]
    public class BusinessCoreController(IBusinessCoreService businessCoreService, IUnitOfWork unitOfWork) : ControllerBase {

        [HttpPost]
        [Route("Create-Employee")]
        public async Task<IActionResult> CreateNewEmployee(EmployeeCreationDto createEmployeeDto) {

            var result = await businessCoreService.CreateNewEmployee(createEmployeeDto);
            return Ok(result);

        }

        [HttpPut]
        [Route("Assign-User")]
        public async Task<IActionResult> AssignUserToBranch(int userId, int currentBranchId, int newBranchId) {

            var result = await businessCoreService.AssignUserToBranch(userId, currentBranchId, newBranchId);
            return Ok(result);
        }

    }
}
