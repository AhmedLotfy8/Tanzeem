using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Products {
    public class ProductService : IProductService {
        
        public Task<ProductDto> GetProductByIdAsync(int id) {
            throw new NotImplementedException();
        }
        
        public Task<IEnumerable<ProductDto>> GetAllProductsAsync() {
            throw new NotImplementedException();
        }

        public void CreateProductAsync(ProductDto productDto) {
            throw new NotImplementedException();
        }

        public void CsvUploadAsync(string filePath) {
            throw new NotImplementedException();
        }

        public void UpdateProductAsync(int id, ProductDto productDto) {
            throw new NotImplementedException();
        }

        public void DeletedProductAsync(int id) {
            throw new NotImplementedException();
        }


    }
}
