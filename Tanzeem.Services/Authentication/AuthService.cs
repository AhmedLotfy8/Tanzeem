using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Authentication {
    public class AuthService(IUnitOfWork unitOfWork,
        IOptions<JwtOptions> options) : IAuthService {

        public async Task<int?> SignUp(UserDto userDto) {

            var users = await unitOfWork.GetRepository<User>().GetAllAsync();
            var userCheck = users.FirstOrDefault(u => u.Email == userDto.Email);

            if (userCheck is not null) {
                throw new Exception("Email is already Registered");
            }


            #region Mapping
            var user = new User() {
                Name = userDto.Name,
                Email = userDto.Email,
                Role = (UserRoles)userDto.Role,
                CompanyId = 3, // hardcoded for now, will be dynamic when company registration is implemented
                BURelations = new List<BranchUserRelationship> {
                    new BranchUserRelationship {
                        BranchId = 2, // hardcoded
                        IsPrimary = true,
                    }
                }
            };

            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, userDto.Password);
            user.PasswordHash = hashedPassword;

            #endregion

            await unitOfWork.GetRepository<User>().AddAsync(user);
            var count = await unitOfWork.SaveChangesAsync();

            return user.Id;
        }

        public async Task<string?> Login(UserLoginDto userLoginDto) {

            var users = await unitOfWork.GetRepository<User>().GetAllAsync(u => u.BURelations);
            var user = users.FirstOrDefault(u => u.Email == userLoginDto.Email);

            if (user is null) {
                throw new Exception("User not found");
            }


            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userLoginDto.Password)
                == PasswordVerificationResult.Failed) {
                throw new Exception("Wrong password");
            }

            var token = AuthHelper.GenerateToken(user, options);

            return token;
        }

        

    }

}
