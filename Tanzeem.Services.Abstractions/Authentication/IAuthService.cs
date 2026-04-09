using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Users;

namespace Tanzeem.Services.Abstractions.Authentication {
    public interface IAuthService {
    
        Task<string?> Login(UserDto userDto);

        Task<int?> Register(UserDto userDto);

    }
}
