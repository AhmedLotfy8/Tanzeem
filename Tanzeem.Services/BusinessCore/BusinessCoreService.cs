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

            if (employeeCreationDto.Role == UserRoles.Admin) {
                throw new Exception("Cannot create employee with Admin role.");
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

        public async Task<bool> AssignUserToBranch(int userId, int currentBranchId, int newBranchId) {

            #region Validate entities exist

            var userExists = await unitOfWork.GetRepository<User>().GetAsync(u => u.Id == userId);
            var branchExists = await unitOfWork.GetRepository<Branch>().GetAsync(b => b.Id == newBranchId);
            if (userExists is null || branchExists is null)
                throw new Exception("User or Branch not found.");

            #endregion


            #region Fetch current primary

            var currentPrimaryRelation = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == userId && bur.BranchId == currentBranchId);
            if (currentPrimaryRelation is null)
                throw new Exception("Current primary branch relation not found.");

            #endregion


            currentPrimaryRelation.IsPrimary = false;
            unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(currentPrimaryRelation);

            var newPrimaryRelation = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == userId && bur.BranchId == newBranchId);

            // If the relation already exists, just update it to be primary. 
            if (newPrimaryRelation is not null) {
                newPrimaryRelation.IsPrimary = true;
                unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(newPrimaryRelation);
            }

            // Otherwise, create a new relation.
            else {
                await unitOfWork.GetRepository<BranchUserRelationship>().AddAsync(new BranchUserRelationship {
                    UserId = userId,
                    BranchId = newBranchId,
                    IsPrimary = true
                });
            }

            return await unitOfWork.SaveChangesAsync() > 0;
        }
    
    }
}

