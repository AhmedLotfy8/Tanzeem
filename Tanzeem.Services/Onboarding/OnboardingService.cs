using Tanzeem.Domain.Contracts;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Services.Abstractions.Branches;
using Tanzeem.Services.Abstractions.Companies;
using Tanzeem.Services.Abstractions.Onboarding;
using Tanzeem.Shared.Dtos.Onboarding;

namespace Tanzeem.Services.Onboarding {
    public class OnboardingService(IUnitOfWork unitOfWork,
        IAuthService authService,
        ICompanyService companyService,
        IBranchService branchService) : IOnboardingService {

        // Hardcoded values
        public async Task<string> OnboardNewTenantAsync(OnboardingDto onboardingDto) {

            if (onboardingDto == null) {
                throw new ArgumentNullException(nameof(onboardingDto), "Onboarding data cannot be null.");
            }

            var transaction = await unitOfWork.BeginTransactionAsync();
            try {

                #region Create new admin

                var adminDto = onboardingDto.SignUpDto;
                var adminId = await authService.CreateAdminAsync(adminDto);

                #endregion


                #region Create new company
                var companyDto = onboardingDto.CompanyDto;

                var companyId = await companyService.CreateNewCompanyAsync(companyDto, adminId);
                #endregion


                #region Create new branch
                var branchDto = onboardingDto.BranchDto;

                var branchId = await branchService.CreateNewBranchAsync(branchDto, adminId, companyId);

                #endregion

                await transaction.CommitAsync();
                return await Task.FromResult($"Onboarding successful! Admin ID: {adminId}, Company ID: {companyId}, Branch ID: {branchId}");
            }


            catch (Exception ex) { // Handle exceptions (e.g., log the error, return a failure response, etc.)
                await transaction.RollbackAsync();
                return await Task.FromResult($"Onboarding failed: {ex.Message}");
            }

        }

    }
}
