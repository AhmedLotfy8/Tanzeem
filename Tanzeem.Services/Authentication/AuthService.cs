using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Shared;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Authentication {
    public class AuthService(IUnitOfWork unitOfWork,
        IOptions<JwtOptions> options) : IAuthService {

        public async Task<int?> Register(UserDto userDto) {

            var users = await unitOfWork.GetRepository<User>().GetAllAsync();
            var userCheck = users.FirstOrDefault(u => u.Email == userDto.Email);

            if (userCheck is not null) {
                throw new Exception("Email is already Registered");
            }

            #region Mapping
            var user = new User() {
                Name = userDto.Name,
                Email = userDto.Email,
                Role = UserRoles.Admin,
                CompanyId = 3 // hardcoded for now, will be dynamic when company registration is implemented
            };

            var hashedPassword = new PasswordHasher<User>()
                .HashPassword(user, userDto.Password);
            user.PasswordHash = hashedPassword;

            #endregion

            await unitOfWork.GetRepository<User>().AddAsync(user);
            var count = await unitOfWork.SaveChangesAsync();

            return user.Id;
        }

        public async Task<string?> Login(UserDto userDto) {

            var users = await unitOfWork.GetRepository<User>().GetAllAsync();
            var user = users.FirstOrDefault(u => u.Email == userDto.Email);

            if (user is null) {
                throw new Exception("User not found");
            }


            if (new PasswordHasher<User>().VerifyHashedPassword(user, user.PasswordHash, userDto.Password)
                == PasswordVerificationResult.Failed) {
                throw new Exception("Wrong password");
            }

            var token = AuthHelper.GenerateToken(user, options);

            return token;
        }


    }

}
