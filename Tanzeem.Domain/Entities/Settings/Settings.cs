using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Branches;

namespace Tanzeem.Domain.Entities.Settings
{
    public class Setting
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }

        public int BranchId { get; set; }
        public Branch Branch { get; set; }
    }
}
