using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Users;


namespace Tanzeem.Services.Abstractions.BusinessCore {
    public interface IBusinessCore {
        Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto);
        Task<int> CreateExtraBranch(BranchDto branchDto);
        Task<bool> AssignUserToBranch(int userId, int BranchId); // Admins or Employees

    }
}
