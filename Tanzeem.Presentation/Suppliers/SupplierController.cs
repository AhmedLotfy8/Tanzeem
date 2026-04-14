using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Shared.Dtos.Suppliers;
using static System.Net.Mime.MediaTypeNames;

namespace Tanzeem.Presentation.Suppliers
{

    [ApiController]
    [Route("api/[controller]")]
    public class SupplierController(ISupplierService _supplierService) : ControllerBase
    {
        [HttpPost]
     //   [Route("suppliers/")]
        public async Task<IActionResult> AddSupplier(SupplierRequestDto supplierDto)
        {
            var result = await _supplierService.CreateSupplierAsync(supplierDto);
            return Ok(result);
        }

        [HttpDelete]
       // [Route("suppliers/")]
        public async Task<IActionResult> RemoveSupplier(int id)
        {
            var result = await _supplierService.DeleteSupplierAsync(id);
            if (result == true)
            {
                return Ok(result);
            }
            else
            {
                return NotFound("This Supplier Not found");
            }
        }

        [HttpGet]
       // [Route("suppliers/{id}")]
        public IActionResult DisplayAllSuppliers()
        {
            var result = _supplierService.GetAllSuppliersAsync();

            if (result.Any())
                return Ok(result);
            else
                return NoContent();
        }
        
        [HttpGet("{id}")]
      //  [Route("suppliers/{id}")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id);

            if (result == null) return NotFound("This Supplier Not found");

            return Ok(result);
        }

        [HttpPut]
      //  [Route("suppliers/{id}")]
        public async Task<IActionResult> UpdateSupplier(SupplierRequestDto supplierDto,int id)
        {
            var result = await _supplierService.UpdateSupplierAsync(id,supplierDto);

            return Ok(result);
        }

        [HttpGet("lookup")]
        public async Task<IActionResult> getSupplierNames()
        {
            var result = await _supplierService.GetSuppliersLookupAsync();
            return Ok(result);
        }


    }
}
