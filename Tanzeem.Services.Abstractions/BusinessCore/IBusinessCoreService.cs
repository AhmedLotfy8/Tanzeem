using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Users;


namespace Tanzeem.Services.Abstractions.BusinessCore {
    public interface IBusinessCoreService {
        Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto);
        Task<bool> AssignUserToBranch(int userId, int currentBranchId, int newBranchId); // Admins or Employees

    }
}
