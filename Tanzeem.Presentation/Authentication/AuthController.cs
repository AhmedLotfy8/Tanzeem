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

        [HttpGet]
        [Route("Get-Profile")]
        public async Task<IActionResult> GetProfile() {
            var profile = await authService.GetUserProfileAsync();
            return Ok(profile);
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto) {
            var token = await authService.Login(userLoginDto);
            return Ok(token);
        }



    }
}
