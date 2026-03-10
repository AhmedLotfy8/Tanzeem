using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Shared.Dtos.Branches;

namespace Tanzeem.Services.Branches {
    public class BranchService(IUnitOfWork _unitOfWork) :
        IBranchService {

        public async Task<BranchDto> GetBranchAsync(int branchId) {

            var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

            if (branch == null) {
                throw new Exception("Branch not found");
            }

            if (branch.Status != BranchStatus.Active) {
                throw new Exception("Branch is not active");
            }

            #region Mapping
            var result = new BranchDto {
                Id = branch.Id,
                Name = branch.Name,
                Location = branch.Location,
                PhoneNumber = branch.PhoneNumber,
                Email = branch.Email,
                CreatedAt = branch.CreatedAt,
                Status = branch.Status.ToString()
            };
            #endregion

            return result;
        }

        public async Task<int> SetCurrentBranchAsync(int branchId) {

            #region Notes
            // This function is currently useless
            // Until we implement user authentication and session management, we cannot set a current branch for the user
            // Later the function will updated branch context
            #endregion

            var branch = await GetBranchAsync(branchId);

            return branch.Id;
        }

        public async Task<List<BranchDto>> GetCompanyBranchesAsync(int companyId) { // Branches List

            var branches = await _unitOfWork.GetRepository<Branch>().GetAllAsync();

            var result = branches.Where(b => b.CompanyId == companyId)
                .Select(branch => new BranchDto {
                    Id = branch.Id,
                    Name = branch.Name,
                    Location = branch.Location,
                    PhoneNumber = branch.PhoneNumber,
                    Email = branch.Email,
                    CreatedAt = branch.CreatedAt,
                    Status = branch.Status.ToString()
                }).ToList();

            return result;
        }

        public async Task<int> CreateNewBranchAsync(BranchDto branchDto) {

            #region Mapping
            var branch = new Branch {
                Name = branchDto.Name,
                Location = branchDto.Location,
                PhoneNumber = branchDto.PhoneNumber,
                Email = branchDto.Email,
                CreatedAt = DateTime.UtcNow,
                CompanyId = 3 // This is hardcoded for now, later we will get the company id from the user context
            };
            #endregion

            await _unitOfWork.GetRepository<Branch>().AddAsync(branch);
            var count = await _unitOfWork.SaveChangesAsync();

            return branch.Id;
        }

        public async Task<int> UpdateBranchAsync(int branchId, BranchDto branchDto) {

            var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

            if (branch == null) {
                throw new Exception("Branch not found");
            }

            #region Mapping
            branch.Name = branchDto.Name;
            branch.Location = branchDto.Location;
            branch.PhoneNumber = branchDto.PhoneNumber;
            branch.Email = branchDto.Email;
            branch.Status = Enum.Parse<BranchStatus>(branchDto.Status);
            #endregion

            var count = await _unitOfWork.SaveChangesAsync();
            return branch.Id;
        }

        public async Task<bool> DeleteBranchAsync(int branchId) {

            var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

            if (branch == null) {
                throw new Exception("Branch not found");
            }

            branch.Status = BranchStatus.Closed;
            var count = await _unitOfWork.SaveChangesAsync();

            return count > 0;
        }


        #region Might use

        //Task<bool> SetBranchActivity(int branchId);

        //public async Task<bool> SetBranchActivity(int branchId) {

        //    var branch = await _unitOfWork.GetRepository<Branch>().GetByIdAsync(branchId);

        //    if (branch == null) {
        //        throw new Exception("Branch not found");
        //    }

        //    if (branch.Status == BranchStatus.Active) {
        //        branch.Status = BranchStatus.Inactive;
        //        return false;
        //    }

        //    else {
        //        branch.Status = BranchStatus.Active;
        //        return true;
        //    }

        //}

        #endregion

    }
}


