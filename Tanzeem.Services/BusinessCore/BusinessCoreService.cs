using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.BusinessCore {
    public class BusinessCoreService(
        IUnitOfWork unitOfWork,
        IBranchService branchService,
        ICurrentService currentService) : IBusinessCoreService {

        public async Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == employeeCreationDto.Email);

            if (user is not null) {
                throw new BusinessRuleException("Email is already Registered!");
            }

            if (employeeCreationDto.Role == UserRoles.Admin) {
                throw new BusinessRuleException("Cannot create employee with Admin role.");
            }


            #region Mapping
            var employee = new User() {
                UserId = Guid.NewGuid().ToString("N")[..8],
                Name = employeeCreationDto.Name,
                Email = employeeCreationDto.Email,
                Role = employeeCreationDto.Role, // Staff / Manager
                PhoneNumber = employeeCreationDto.PhoneNumber ?? string.Empty,
                Status = UserStatus.Active,
                CompanyId = currentService.CompanyId,
                BURelations = new List<BranchUserRelationship> {
                    new BranchUserRelationship {
                        BranchId = currentService.BranchId ?? 0,
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

        public async Task<int> CreateAdditionalBranchAsync(BranchDto branchDto) {
            var adminId = currentService.UserId
                ?? throw new InvalidOperationException("User not authenticated.");
            var companyId = currentService.CompanyId
                ?? throw new InvalidOperationException("Company context missing from token.");

            return await branchService.CreateNewBranchAsync(branchDto, adminId, companyId);
        }

        public async Task<bool> AssignUserToBranch(int userId, int newBranchId) {
            var userTask = unitOfWork.GetRepository<User>().GetAsync(u => u.Id == userId);
            var branchTask = unitOfWork.GetRepository<Branch>().GetAsync(b => b.Id == newBranchId);
            await Task.WhenAll(userTask, branchTask);

            if (userTask.Result is null || branchTask.Result is null)
                throw new Exception("User or Branch not found.");

            var currentPrimaryRelation = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == userId && bur.IsPrimary);

            if (currentPrimaryRelation is null)
                throw new Exception("Current primary branch relation not found.");

            if (currentPrimaryRelation.BranchId == newBranchId) return true;

            currentPrimaryRelation.IsPrimary = false;
            unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(currentPrimaryRelation);

            var newPrimaryRelation = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == userId && bur.BranchId == newBranchId);

            if (newPrimaryRelation is not null) {
                newPrimaryRelation.IsPrimary = true;
                unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(newPrimaryRelation);
            }
            else {
                await unitOfWork.GetRepository<BranchUserRelationship>().AddAsync(new BranchUserRelationship {
                    UserId = userId,
                    BranchId = newBranchId,
                    IsPrimary = true
                });
            }

            return await unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<UserProfileDto> GetUserProfileAsync() {
            var user = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == currentService.UserId);
            if (user is null) {
                throw new Exception("User not found");
            }
            var userProfile = MapToProfileDto(user);
            return userProfile;

        }

        public async Task<UserProfileDto> GetEmployeeProfileAsync(int id) {
            var employee = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == id && u.Role != UserRoles.Admin);
            if (employee is null) {
                throw new Exception("Employee not found");
            }
            var employeeProfile = MapToProfileDto(employee);
            return employeeProfile;
        }        

        public async Task<List<UserProfileDto>> GetAllEmployeesAsync() {
            var employeeProfiles = await unitOfWork.GetRepository<User>()
                .GetAllAsIQueryable()
                .Where(u => u.Role != UserRoles.Admin && u.Status == UserStatus.Active)
                .Select(u => MapToProfileDto(u))
                .ToListAsync();

            return employeeProfiles;

        }

        public async Task<bool> TerminateEmployeeAsync(int employeeId) {
            var employee = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == employeeId && u.Role != UserRoles.Admin);
            if (employee is null) {
                throw new Exception("Employee not found");
            }

            employee.Status = UserStatus.Inactive;
            unitOfWork.GetRepository<User>().UpdateAsync(employee);
            return await unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateEmployeeAsync(int employeeId, EmployeeUpdateDto updatedEmployeeDto) {
            var employee = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == employeeId && u.Role != UserRoles.Admin);
            if (employee is null) {
                throw new Exception("User not found");
            }

            employee.Name = updatedEmployeeDto.Name;
            employee.Email = updatedEmployeeDto.Email;
            employee.PhoneNumber = updatedEmployeeDto.PhoneNumber ?? employee.PhoneNumber;
            employee.Role = updatedEmployeeDto.Role;
            if (!string.IsNullOrEmpty(updatedEmployeeDto.tempPassword)) {
                var hashedPassword = new PasswordHasher<User>()
                    .HashPassword(employee, updatedEmployeeDto.tempPassword);
                employee.PasswordHash = hashedPassword;
            }
            unitOfWork.GetRepository<User>().UpdateAsync(employee);
            return await unitOfWork.SaveChangesAsync() > 0;

        }

        private static UserProfileDto MapToProfileDto(User user) => new() {
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            Role = user.Role
        };


    }
}

