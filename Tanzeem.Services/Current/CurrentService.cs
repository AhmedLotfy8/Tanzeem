using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Current {
    public class CurrentService(IHttpContextAccessor httpContextAccessor) : ICurrentService {
        public int UserId => int.Parse(GetClaim(ClaimTypes.NameIdentifier)!);

        public int CompanyId => int.Parse(GetClaim("CompanyId")!);

        public int BranchId => int.Parse(GetClaim("BranchId")!);

        public string Role => ClaimTypes.Role;

        private string GetClaim(string claimType) =>
            httpContextAccessor.HttpContext?.User?.FindFirst(claimType)?.Value;
    }
}
