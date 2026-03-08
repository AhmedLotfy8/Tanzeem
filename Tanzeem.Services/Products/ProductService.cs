using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Companies;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Products {
    public class ProductService(IUnitOfWork _unitOfWork) : IProductService {
        
        public Task<ProductDto> GetProductByIdAsync(int id) {

            var product = _unitOfWork.GetRepository<Product>()
                .GetByIdAsync(id).Result;


            if (product is null) {
                throw new Exception("Product not found");
            }


            #region Mapping
            var result = new ProductDto {
                Name = product.Name,
                SKU = product.SKU,
                Category = "TempCat",
                Stock = _unitOfWork.GetRepository<Inventory>()
                    .GetAllAsync().Result.FirstOrDefault(i => i.ProductId == product.Id)?.Quantity ?? 0,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,
                
            };
            #endregion


            return Task.FromResult(result);
        }

        public Task<IEnumerable<ProductDto>> GetAllProductsAsync() {

            #region Mapping
            var result = _unitOfWork.GetRepository<Product>()
                .GetAllAsync().Result.Select(product => new ProductDto {
                    Name = product.Name,
                    SKU = product.SKU,
                    Category = "TempCat",
                    Stock = _unitOfWork.GetRepository<Inventory>()
                        .GetAllAsync().Result.FirstOrDefault(i => i.ProductId == product.Id)?.Quantity ?? 0,
                    CostPrice = product.CostPrice,
                    SellingPrice = product.SellingPrice,
                    ExpiryDate = product.ExpiryDate,
                    Barcode = product.Barcode,
                    Description = product.Description,
                    ReorderLevel = product.ReorderLevel,
                    Status = product.Status
                });
            #endregion

            return Task.FromResult(result);
        }

        public async Task<int> CreateProductAsync(ProductDto productDto) {

            #region Category Retrieval and Assigning
            var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();
            var category = categories.FirstOrDefault(c => c.Name == productDto.Category);

            if (category == null) {
                category = new Category { Name = productDto.Category };
                await _unitOfWork.GetRepository<Category>().AddAsync(category);
                await _unitOfWork.SaveChangesAsync();
            }
            #endregion

            #region Mapping and Adding
            var product = new Product {
                Name = productDto.Name,
                SKU = productDto.SKU,
                CostPrice = productDto.CostPrice,
                SellingPrice = productDto.SellingPrice,
                ExpiryDate = productDto.ExpiryDate,
                Barcode = productDto.Barcode,
                Description = productDto.Description,
                ReorderLevel = productDto.ReorderLevel,
                Status = productDto.Status,
                Category = category,
                CompanyId = 3,
                
            };
            await _unitOfWork.GetRepository<Product>().AddAsync(product);

            await _unitOfWork.GetRepository<Inventory>().AddAsync(new Inventory {
                Product = product,
                Quantity = productDto.Stock,
                BranchId = 1

            });
            #endregion

            var count = await _unitOfWork.SaveChangesAsync();
            return product.Id;
        }

        public Task<int> CsvUploadAsync(string filePath) {
            throw new NotImplementedException();
        }

        public async Task<int> UpdateProductAsync(int id, ProductDto productDto) {

            var product = await _unitOfWork.GetRepository<Product>().GetByIdAsync(id);

            #region Mapping

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.SKU = productDto.SKU;
            product.CostPrice = productDto.CostPrice;
            product.SellingPrice = productDto.SellingPrice;
            product.ExpiryDate = productDto.ExpiryDate;
            product.Barcode = productDto.Barcode;
            product.ReorderLevel = productDto.ReorderLevel;
            product.Status = productDto.Status;

            #region Category Mapping
            var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();
            var categroyCheck = categories.FirstOrDefault(c => c.Name == productDto.Category);

            if (categroyCheck is null) {

                var newCategory = new Category { Name = productDto.Category };
                await _unitOfWork.GetRepository<Category>().AddAsync(newCategory);
                product.Category = newCategory;
            }
            else {
                product.Category = categroyCheck;
            }
            #endregion

            #endregion

            var count = await _unitOfWork.SaveChangesAsync();
            return product.Id;
        }

        public async Task<bool> DeletedProductAsync(int id) {

            var test = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
            var test2 = test.FirstOrDefault(i => i.ProductId == id);

            _unitOfWork.GetRepository<Inventory>().DeleteAsync(test2);
            var result = await _unitOfWork.GetRepository<Product>().GetByIdAsync(id);    
            _unitOfWork.GetRepository<Product>().DeleteAsync(result);
            var count = await _unitOfWork.SaveChangesAsync();
            return count > 0;
        }

    }

}
