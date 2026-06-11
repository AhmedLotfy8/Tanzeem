using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Authentication;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Presentation.Authentication {

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IAuthService authService) : ControllerBase {


        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto) {
            var token = await authService.Login(userLoginDto);
            return Ok(token);
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto dto)
        {
            var result = await authService.ChangePasswordAsync(dto);
            return Ok(new { success = result });
        }

    }
}
