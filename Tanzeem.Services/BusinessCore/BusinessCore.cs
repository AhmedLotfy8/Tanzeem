using Microsoft.Extensions.Options;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.BusinessCore {
    public class BusinessCore(IUnitOfWork _unitOfWork, IOptions<JwtOptions> options) : IBusinessCore {

        public Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto) {
            throw new NotImplementedException();
        }
        
        public Task<int> CreateExtraBranch(BranchDto branchDto) {
            throw new NotImplementedException();
        }

        public Task<bool> AssignUserToBranch(int userId, int BranchId) {
            throw new NotImplementedException();
        }

    }
}

