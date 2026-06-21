using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions;
using Subscription = Tanzeem.Domain.Entities.Subscriptions.Subscription;

namespace Tanzeem.Presentation.Payments {

    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController(IPaymentService paymentService,
        IUnitOfWork unitOfWork) : ControllerBase {

        [HttpPost]
        [Route("Subscribe")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePaymentIntent() {
            try {
                var createSubscriptionResultDto = await paymentService.CreateSubscriptionAsync(PlanType.Professional);
                return Ok(createSubscriptionResultDto);
            }
            catch (Exception ex) {
                return BadRequest(new { message = ex.Message });
            }
        }

        // https://tanzeem.runasp.net/api/payments/webhook
        [HttpPost]
        [Route("webhook")]
        public async Task<IActionResult> Index() {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            const string endpointSecret = "whsec_PmI6PR4NIFcVxkUPCdjy4QfxMRDfPcQg";

            var stripeEvent = EventUtility.ParseEvent(json);
            var signatureHeader = Request.Headers["Stripe-Signature"];

            stripeEvent = EventUtility.ConstructEvent(json,
                    signatureHeader, endpointSecret);

            if (stripeEvent.Type == EventTypes.InvoicePaymentSucceeded) {
                var invoice = stripeEvent.Data.Object as Invoice;
                var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
                var subscription = await unitOfWork.GetRepository<Subscription>()
                    .GetAsync(s => s.StripeSubscriptionId == subscriptionId);
                if (subscription is null) return BadRequest();
                subscription.Status = SubscriptionStatus.Active;
                subscription.CurrentPeriodEnd = invoice.PeriodEnd;
                await unitOfWork.SaveChangesAsync();
            }
            else if (stripeEvent.Type == EventTypes.InvoicePaymentFailed) {
                var invoice = stripeEvent.Data.Object as Invoice;
                var subscriptionId = invoice.Parent?.SubscriptionDetails?.SubscriptionId;
                var subscription = await unitOfWork.GetRepository<Subscription>()
                    .GetAsync(s => s.StripeSubscriptionId == subscriptionId);
                if (subscription is null) return BadRequest();
                subscription.Status = SubscriptionStatus.PastDue;
                await unitOfWork.SaveChangesAsync();
            }
            else if (stripeEvent.Type == EventTypes.CustomerSubscriptionDeleted) {
                var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                var entity = await unitOfWork.GetRepository<Subscription>()
                    .GetAsync(s => s.StripeSubscriptionId == subscription.Id);
                if (entity is null) return BadRequest();
                entity.Status = SubscriptionStatus.Canceled;
                await unitOfWork.SaveChangesAsync();
            }
            else {
                Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
            }

            return Ok();
        }



    }

}
