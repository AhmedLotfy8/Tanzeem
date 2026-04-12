using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Users {
    public class UserDto {
    
        public string Name { get; set; }
    
        public string Email { get; set; }
        
        public string Password { get; set; }

        public int Role { get; set; }

    }
}
