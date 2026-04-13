using Microsoft.AspNetCore.Mvc;
using Tanzeem.Services.Abstractions.Onboarding;

namespace Tanzeem.Presentation.Onboarding {

    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController(IOnboardingService onboardingService) : ControllerBase {
    


    }
}
