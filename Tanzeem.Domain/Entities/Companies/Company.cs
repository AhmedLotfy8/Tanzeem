using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Entities.Companies {
    public class Company {
    
        public int Id { get; set; }

        public string Field { get; set; }         // Agriculture, Manufacturing, Pharmaceutical, etc.

    }
}
