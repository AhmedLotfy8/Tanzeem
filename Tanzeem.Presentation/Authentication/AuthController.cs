using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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

        [HttpPost]
        [Route("Admin-Register")]
        public async Task<IActionResult> UserRegister(AdminSignUpDto userDto) {
            var userId = await authService.CreateAdminAsync(userDto);
            return Ok(userId);
        }

    }
}
