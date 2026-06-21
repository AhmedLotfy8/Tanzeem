using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Subscriptions;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions;
using Tanzeem.Shared.Dtos.Subscription;
using Subscription = Tanzeem.Domain.Entities.Subscriptions.Subscription;
using StripeSubscriptionService = Stripe.SubscriptionService;
using Microsoft.Extensions.Configuration;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Payment {
    public class PaymentService(IUnitOfWork unitOfWork,
        ICurrentService currentService,
        IConfiguration configuration) : IPaymentService {
        public async Task<CreateSubscriptionResultDto> CreateSubscriptionAsync(PlanType plan) {

            #region App settings
            
            StripeConfiguration.ApiKey = configuration["StripeOptions:SecretKey"];
            var priceId = configuration[$"StripeOptions:PlanPrices:{plan}"];

            if (string.IsNullOrEmpty(priceId))
                throw new BusinessRuleException($"No price configured for plan {plan}.");

            #endregion

            var companyId = currentService.CompanyId;
            if (companyId is null)
                throw new BusinessRuleException("Current user does not have a company.");

            var existing = await unitOfWork.GetRepository<Subscription>()
                .GetAsync(s => s.CompanyId == companyId);

            var customerService = new CustomerService();
            string stripeCustomerId = existing?.StripeCustomerId
                ?? (await customerService.CreateAsync(new CustomerCreateOptions { /* email, name */ })).Id;

            var subscriptionService = new StripeSubscriptionService();
            var stripeSubscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions {
                Customer = stripeCustomerId,
                Items = new List<SubscriptionItemOptions> { new() { Price = priceId } },
                PaymentBehavior = "default_incomplete",
                Expand = new List<string> { "latest_invoice.payment_intent" }
            });

            var entity = existing ?? new Subscription { CompanyId = companyId.Value };
            entity.StripeCustomerId = stripeCustomerId;
            entity.StripeSubscriptionId = stripeSubscription.Id;
            entity.Plan = plan;
            entity.Status = SubscriptionStatus.Incomplete; // webhook flips this to Active once paid

            if (existing is null)
                await unitOfWork.GetRepository<Subscription>().AddAsync(entity);

            await unitOfWork.SaveChangesAsync();

            var clientSecret = stripeSubscription.LatestInvoice.ConfirmationSecret?.ClientSecret;
            if (string.IsNullOrEmpty(clientSecret))
                throw new BusinessRuleException("Stripe did not return a client secret for this subscription.");


            return new CreateSubscriptionResultDto {
                SubscriptionId = entity.Id,
                ClientSecret = clientSecret
            };
    
        }


        #region Unused
        private async Task UpdateSubscriptionStatusAsync(string stripeSubscriptionId, SubscriptionStatus status, DateTime? periodEnd) {
            var subscription = await unitOfWork.GetRepository<Subscription>()
                .GetAsync(s => s.StripeSubscriptionId == stripeSubscriptionId);

            if (subscription is null) return; // ignore unknown subscriptions

            subscription.Status = status;
            if (periodEnd.HasValue)
                subscription.CurrentPeriodEnd = periodEnd.Value;

            await unitOfWork.SaveChangesAsync();
        }

        #endregion

    }

}
