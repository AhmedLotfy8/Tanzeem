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
        void CreateProductAsync(ProductDto productDto);
        void CsvUploadAsync(string filePath);

        // Put
        void UpdateProductAsync(int id, ProductDto productDto);

        // Delete
        void DeletedProductAsync(int id);

    }
}
