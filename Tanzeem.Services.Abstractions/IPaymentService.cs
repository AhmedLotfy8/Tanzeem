using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Enums;
using Tanzeem.Shared.Dtos.Subscription;

namespace Tanzeem.Services.Abstractions {
    public interface IPaymentService {

        Task<CreateSubscriptionResultDto> CreateSubscriptionAsync(PlanType plan);

    }
}