using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

        public async Task<int> CreateAdminAsync(AdminSignUpDto userDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == userDto.Email);
            
            if (user is not null) {
                throw new Exception("Email is already Registered!");
            }

            #region Mapping
            var admin = new User() {
                Name = userDto.Name,
                Email = userDto.Email,
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

        ///TODO i changed somethings at Login Auth Service
        public async Task<string?> Login(UserLoginDto userLoginDto) {

            var user = await unitOfWork.GetRepository<User>().GetAsync(u => u.Email == userLoginDto.Email);

            if (user is null) {
                throw new Exception("User not found");
            }


            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userLoginDto.Password)
                == PasswordVerificationResult.Failed) {
                throw new Exception("Wrong password");
            }

            var token = await AuthHelper.GenerateToken(user, options, unitOfWork);

            return token;
        }

    }

}


#region Temp -> delete if current code works
//var users = await unitOfWork.GetRepository<User>().GetAllAsync();
//var userCheck = users.FirstOrDefault(u => u.Email == userDto.Email);

//var users = await unitOfWork.GetRepository<User>().GetAllAsync(u => u.BURelations);
//var user = users.FirstOrDefault(u => u.Email == userLoginDto.Email);
#endregion