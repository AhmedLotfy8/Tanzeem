using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Presentation.Products {

    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController(IProductService productService) : ControllerBase {

        [HttpGet]
        [Route("Get-Products")]
        // [Authorize(Roles = "Admin, Manager")]
        public async Task<IActionResult> GetAllProducts(int? sortId, int? filterId) {
            var result = await productService.GetAllProductsAsync(sortId, filterId);
            return Ok(result);
        }

        [HttpPost]
        [Route("Create-Product")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> CreateProduct(ProductDto dto) {
            var result = await productService.CreateProductAsync(dto);
            return Ok(result);
        }

        [HttpGet]
        [Route("Get-Product/{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> GetProductById(int id) {
            var result = await productService.GetProductByIdAsync(id);
            return Ok(result);
        }

        [HttpPut]
        [Route("Update-Product/{id}")]
        //[Authorize(Roles = "")]
        public async Task<IActionResult> UpdateProduct(int id, ProductDto dto) {
            var result = await productService.UpdateProductAsync(id, dto);
            return Ok(result);
        }

        [HttpDelete]
        [Route("Delete-Product/{id}")]    
        //[Authorize(Roles = "")]
        public async Task<IActionResult> DeleteProduct(int id) {
            var result = await productService.DeletedProductAsync(id);
            return Ok(result);
        }

    }
}
