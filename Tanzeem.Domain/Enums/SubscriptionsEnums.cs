using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tanzeem.Domain.Enums {
    public enum SubscriptionStatus {
        Incomplete = 1,      
        Active = 2,
        PastDue = 3,         
        Canceled = 4,
        Unpaid = 5           
    }

    public enum PlanType {
        Basic = 1,
        Professional = 2,
    }

}
