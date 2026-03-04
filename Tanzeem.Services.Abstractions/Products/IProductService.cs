using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Abstractions.Products {
    public interface IProductService {

        // Get
        Task<ProductDto> GetProductByIdAsync(int id);
        Task<IEnumerable<ProductDto>> GetAllProductsAsync();

        // Post
        Task CreateProductAsync(ProductDto productDto);
        Task CsvUploadAsync(string filePath);

        // Put
        Task UpdateProductAsync(int id, ProductDto productDto);

        // Delete
        Task DeletedProductAsync(int id);

    }
}
