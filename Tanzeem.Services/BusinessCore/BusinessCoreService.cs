using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Settings;
using Tanzeem.Services.Authentication;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.BusinessCore {
    public class BusinessCoreService(
        IUnitOfWork unitOfWork,
        IBranchService branchService,
        IAlertConfigurationsService alertConfigurationsService,
        IAIConfigService aIConfigService,
        ICurrentService currentService,
        IOptions<JwtOptions> jwtOptions) : IBusinessCoreService {

        public async Task<int> CreateNewEmployee(EmployeeCreationDto employeeCreationDto) {

            var existingUser = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Email == employeeCreationDto.Email);

            if (existingUser is not null)
                throw new Exception("Email is already registered.");

            if (employeeCreationDto.Role == UserRoles.Admin)
                throw new BusinessRuleException("Cannot create an employee with Admin role.");

            var employee = new User {
                UserId = Guid.NewGuid().ToString("N")[..8],
                Name = employeeCreationDto.Name,
                Email = employeeCreationDto.Email,
                Role = employeeCreationDto.Role,
                PhoneNumber = employeeCreationDto.PhoneNumber ?? string.Empty,
                Status = UserStatus.Active,
                CompanyId = currentService.CompanyId,
                BURelations = new List<BranchUserRelationship> {
                    new BranchUserRelationship {
                        BranchId = currentService.BranchId ?? 0,
                        IsPrimary = true
                    }
                }
            };

            employee.PasswordHash = new PasswordHasher<User>()
                .HashPassword(employee, employeeCreationDto.tempPassword);

            await unitOfWork.GetRepository<User>().AddAsync(employee);
            await unitOfWork.SaveChangesAsync();

            return employee.Id;
        }

        public async Task<int> CreateAdditionalBranchAsync(BranchDto branchDto) {

            var adminId = currentService.UserId
                ?? throw new InvalidOperationException("User not authenticated.");
            var companyId = currentService.CompanyId
                ?? throw new InvalidOperationException("Company context missing from token.");

            int branchId = await branchService.CreateNewBranchAsync(branchDto, adminId, companyId);

            await alertConfigurationsService.CreateDefaultAlertsConfigurationsAsync(branchId);
            await aIConfigService.CreateAIConfigurations(branchId);

            return branchId;
        }

        public async Task<bool> AssignUserToBranch(int userId, int newBranchId) {
            await SetPrimaryBranchAsync(userId, newBranchId);
            return await unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<string> SwitchBranchAsync(int newBranchId) {
            var adminId = currentService.UserId
                ?? throw new InvalidOperationException("User not authenticated.");

            await SetPrimaryBranchAsync(adminId, newBranchId);
            await unitOfWork.SaveChangesAsync();

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Id == adminId);
            return await AuthHelper.GenerateToken(user!, jwtOptions, unitOfWork);
        }

        public async Task<UserProfileDto> GetUserProfileAsync() {

            var user = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == currentService.UserId);

            if (user is null)
                throw new Exception("User not found.");

            return MapToProfileDto(user);
        }

        public async Task<UserProfileDto> GetEmployeeProfileAsync(int id) {

            var employee = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == id && u.Role != UserRoles.Admin);

            if (employee is null)
                throw new Exception("Employee not found.");

            return MapToProfileDto(employee);
        }

        public async Task<List<UserProfileDto>> GetAllEmployeesAsync() {
            var branchId = currentService.BranchId;

            return await unitOfWork.GetRepository<User>()
                .GetAllAsIQueryable()
                .Where(u => u.Role != UserRoles.Admin
                    && u.Status == UserStatus.Active
                    && u.BURelations.Any(bur => bur.BranchId == branchId && bur.IsPrimary))
                .Select(u => MapToProfileDto(u))
                .ToListAsync();
        }
        
        public async Task<bool> TerminateEmployeeAsync(int employeeId) {

            var employee = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == employeeId && u.Role != UserRoles.Admin);

            if (employee is null)
                throw new Exception("Employee not found.");

            employee.Status = UserStatus.Inactive;
            unitOfWork.GetRepository<User>().UpdateAsync(employee);
            return await unitOfWork.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateEmployeeAsync(int employeeId, EmployeeUpdateDto updatedEmployeeDto) {

            var employee = await unitOfWork.GetRepository<User>()
                .GetAsync(u => u.Id == employeeId && u.Role != UserRoles.Admin);

            if (employee is null)
                throw new Exception("Employee not found.");

            employee.Name = updatedEmployeeDto.Name;
            employee.Email = updatedEmployeeDto.Email;
            employee.PhoneNumber = updatedEmployeeDto.PhoneNumber ?? employee.PhoneNumber;
            employee.Role = updatedEmployeeDto.Role;

            if (!string.IsNullOrEmpty(updatedEmployeeDto.tempPassword)) {
                employee.PasswordHash = new PasswordHasher<User>()
                    .HashPassword(employee, updatedEmployeeDto.tempPassword);
            }

            unitOfWork.GetRepository<User>().UpdateAsync(employee);
            return await unitOfWork.SaveChangesAsync() > 0;
        }

        private async Task SetPrimaryBranchAsync(int userId, int newBranchId) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Id == userId);
            var branch = await unitOfWork.GetRepository<Branch>().GetAsync(b => b.Id == newBranchId);

            if (user is null)
                throw new BusinessRuleException("User not found.");

            if (branch is null)
                throw new BusinessRuleException("Branch not found.");

            if (branch.CompanyId != currentService.CompanyId)
                throw new BusinessRuleException("Branch does not belong to your company.");

            var currentPrimaryRelation = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == userId && bur.IsPrimary);

            if (currentPrimaryRelation is null)
                throw new BusinessRuleException("Current primary branch relation not found.");

            if (currentPrimaryRelation.BranchId == newBranchId)
                return;

            currentPrimaryRelation.IsPrimary = false;
            unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(currentPrimaryRelation);

            var existingRelation = await unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAsync(bur => bur.UserId == userId && bur.BranchId == newBranchId);

            if (existingRelation is not null) {
                existingRelation.IsPrimary = true;
                unitOfWork.GetRepository<BranchUserRelationship>().UpdateAsync(existingRelation);
            }
            else {
                await unitOfWork.GetRepository<BranchUserRelationship>().AddAsync(new BranchUserRelationship {
                    UserId = userId,
                    BranchId = newBranchId,
                    IsPrimary = true
                });
            }
        }

        private static UserProfileDto MapToProfileDto(User user) => new() {
            Id = user.Id,
            UserId = user.UserId,
            Name = user.Name,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Status = user.Status,
            Role = user.Role
        };
    }
}