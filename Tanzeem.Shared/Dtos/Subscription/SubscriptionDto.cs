using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Shared.Dtos.Subscription {
    public class SubscriptionDto {
        public int Id { get; set; }
        public string Plan { get; set; }
        public string Status { get; set; }
        public DateTime CurrentPeriodEnd { get; set; }
    }

}
