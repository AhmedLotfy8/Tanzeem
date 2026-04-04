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
        public async Task<IActionResult> AddSupplier(SupplierDto supplierDto)
        {
            var result = await _supplierService.CreateSupplierAsync(supplierDto);
            return Ok(result);
        }

        [HttpDelete]
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
        public async Task<IActionResult> DisplayAllSuppliers()
        {
            var result = await _supplierService.GetAllSuppliersAsync();

            if (result.Any())
                return Ok(result);
            else
                return NoContent();
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetSupplierById(int id)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id);

            if (result != null) return NotFound("This Supplier Not found");

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSupplier(SupplierDto supplierDto,int id)
        {
            var result = await _supplierService.GetSupplierByIdAsync(id);

            if (result == null) return NotFound("This Supplier Not found");

            return Ok(result);
        }
                
    }
}
