using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.AuditLogs;

namespace Tanzeem.Domain.Entities.Products {
    public class Category : IAuditable{
    
        public int Id { get; set; }

        public string Name { get; set; }

    }
}
