using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Authentication {
    public class AuthService(IUnitOfWork unitOfWork,
        ICurrentService currentService,
        IOptions<JwtOptions> options) : IAuthService {

        public async Task<int> CreateAdminAsync(AdminSignUpDto userDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == userDto.Email);

            if (user is not null) {
                throw new BusinessRuleException("Email is already Registered!");
            }

            #region Mapping
            var admin = new User() {
                UserId = Guid.NewGuid().ToString("N")[..8],
                Name = userDto.Name,
                Email = userDto.Email,
                PhoneNumber = userDto.PhoneNumber,
                Status = UserStatus.Active,
                Role = UserRoles.Admin,
            };

            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(admin, userDto.Password);
            admin.PasswordHash = hashedPassword;

            #endregion

            await unitOfWork.GetRepository<User>().AddAsync(admin);
            var count = await unitOfWork.SaveChangesAsync();

            return admin.Id;
        }


        public async Task<string?> Login(UserLoginDto userLoginDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == userLoginDto.Email);

            if (user is null) {
                throw new BusinessRuleException("User not found!");
            }


            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userLoginDto.Password)
                == PasswordVerificationResult.Failed) {
                throw new BusinessRuleException("Email or password is incorrect!");
            }

            var token = await AuthHelper.GenerateToken(user, options, unitOfWork);

            return token;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordDto dto)
        {
            int userId = currentService.UserId ?? throw new UnauthorizedAccessException("no user id assigned");
            
            var user = await unitOfWork.GetRepository<User>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user is null)
                throw new BusinessRuleException("User not found!");

            var hasher = new PasswordHasher<User>();
            var result = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.OldPassword);

            if (result == PasswordVerificationResult.Failed)
                throw new BusinessRuleException("Old password is incorrect!");

            user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);

            unitOfWork.GetRepository<User>().UpdateAsync(user);
            await unitOfWork.SaveChangesAsync();

            return true;
        }
    }

}