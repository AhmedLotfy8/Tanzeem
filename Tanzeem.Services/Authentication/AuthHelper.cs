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
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Shared;

namespace Tanzeem.Services.Authentication {
    public static class AuthHelper {

        public static async Task<string> GenerateToken(User user, IOptions<JwtOptions> options
            , IUnitOfWork unitOfWork) {

            var jwtOptions = options.Value;
            var primaryBranch = await unitOfWork.GetRepository<BranchUserRelationship>().GetAsync(bu => bu.UserId == user.Id);


            var authClaims = new List<Claim>() {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("UserId", user.Id.ToString()),
                new Claim("CompanyId", user.CompanyId.ToString()),
                new Claim("BranchId", primaryBranch?.BranchId.ToString() ?? "")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);


            var tokenDescriptor = new JwtSecurityToken(
                issuer: jwtOptions.Issuer,
                audience: jwtOptions.Audience,
                expires: DateTime.UtcNow.AddDays(jwtOptions.DurationInDays),
                claims: authClaims,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        }


    }
}
