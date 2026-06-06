using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared.Dtos.Branches;

namespace Tanzeem.Services.Branches {
    public class BranchService(
        IUnitOfWork _unitOfWork,
        ICurrentService currentService) : IBranchService {

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

        public async Task<List<BranchDto>> GetCompanyBranchesAsync() {

            var companyId = currentService.CompanyId;
            var branches = await _unitOfWork.GetRepository<Branch>().GetAllAsIQueryable().ToListAsync();

            #region Mapping

            var result = new List<BranchDto>();
            foreach (var branch in branches) {
                if (branch.CompanyId == companyId) {
                    result.Add(new BranchDto {
                        Id = branch.Id,
                        Name = branch.Name,
                        Location = branch.Location,
                        PhoneNumber = branch.PhoneNumber,
                        Email = branch.Email,
                        CreatedAt = branch.CreatedAt,
                        Status = branch.Status.ToString(),
                    });
                }
            }

            #endregion

            return result;
        }

        public async Task<int> CreateNewBranchAsync(BranchDto branchDto, int adminId, int companyId) {

            var branch = new Branch {
                Name = branchDto.Name,
                Location = branchDto.Location,
                PhoneNumber = branchDto.PhoneNumber,
                Email = branchDto.Email,
                CreatedAt = DateTime.UtcNow,
                Status = BranchStatus.Active,
                CompanyId = companyId
            };

            await _unitOfWork.GetRepository<Branch>().AddAsync(branch);

            bool isFirstBranch = !await _unitOfWork.GetRepository<BranchUserRelationship>()
                .GetAllAsIQueryable()
                .AnyAsync(r => r.UserId == adminId);


            branch.BURelations = new List<BranchUserRelationship>() {
                new BranchUserRelationship {
                    UserId = adminId,
                    IsPrimary = isFirstBranch
                }
            };

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


    }

}
