using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.BusinessCore {
    public class BusinessCoreService(IUnitOfWork unitOfWork, IOptions<JwtOptions> options) : IBusinessCoreService {

        // Hard coded function (companyId, BranchId)
        public async Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto) { 

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == employeeCreationDto.Email);

            if (user is not null) {
                throw new Exception("Email is already Registered!");
            }

            #region Mapping
            var employee = new User() {
                Name = employeeCreationDto.Name,
                Email = employeeCreationDto.Email,
                Role = employeeCreationDto.Role, // Staff / Manager
                CompanyId = 3, // hardcoded for now, will be dynamic when company registration is implemented
                BURelations = new List<BranchUserRelationship> {
                    new BranchUserRelationship {
                        BranchId = 13, // hardcoded
                        IsPrimary = true,
                    }
                }
            };

            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(employee, employeeCreationDto.tempPassword);
            employee.PasswordHash = hashedPassword;

            #endregion

            await unitOfWork.GetRepository<User>().AddAsync(employee);
            var count = await unitOfWork.SaveChangesAsync();

            return employee.Id;
        }

        public async Task<bool> AssignUserToBranch(BranchUserRelationship currentPrimaryRelation, int newBranchId) {
          
            // Prevent duplicate assignment
            var existing = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == currentPrimaryRelation.UserId && bur.BranchId == newBranchId);

            if (existing is not null)
                throw new Exception("User is already assigned to this branch.");

            currentPrimaryRelation.IsPrimary = false;
            unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(currentPrimaryRelation);

            // Assign new primary
            var newRelation = new BranchUserRelationship {
                UserId = currentPrimaryRelation.UserId,
                BranchId = newBranchId,
                IsPrimary = true
            };

            await unitOfWork.GetRepository<BranchUserRelationship>().AddAsync(newRelation);
            return await unitOfWork.SaveChangesAsync() > 0;
        }

    }
}

