using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.BusinessCore;
using Tanzeem.Shared.Dtos.Branches;
using Tanzeem.Shared.Dtos.Companies;

namespace Tanzeem.Presentation.BusinessCore {
    [ApiController]
    [Route("api/[controller]")]
    public class BusinessCoreController(IBusinessCoreService businessCore) : ControllerBase {

    }
}
