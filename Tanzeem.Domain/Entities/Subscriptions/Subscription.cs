using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Users;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Domain.Entities.Subscriptions {
    public class Subscription {
        
        public int Id { get; set; }

        public PlanType Plan { get; set; }

        public SubscriptionStatus Status { get; set; }

        public DateTime CurrentPeriodEnd { get; set; }     


        public string StripeCustomerId { get; set; }       
        public string StripeSubscriptionId { get; set; }   
        public int CompanyId { get; set; }
        public Company Company { get; set; }

    }

}
