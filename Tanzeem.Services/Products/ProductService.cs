using Microsoft.EntityFrameworkCore;
using System;
using System.Collections;
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

        public async Task<ProductDto> GetProductByIdAsync(int id) {

            var product = await _unitOfWork.GetRepository<Product>().GetByIdAsync(id);

            //var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
            //var inventory = inventories.FirstOrDefault(i => i.ProductId == id);
            var inventory = await _unitOfWork.GetRepository<Inventory>().GetAsync(i => i.ProductId == id);
            var categoryName = await _unitOfWork.GetRepository<Category>().GetAsync(c => c.Name == product.Category.Name);
            //var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();

            if (product is null || inventory is null) {
                throw new Exception("Product not found");
            }

            #region Mapping
            var result = new ProductDto {
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category.Name ?? "-",
                Stock = inventory.Quantity ?? 0,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,

            };
            #endregion


            return result;
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(int? sortId, int? filterId) {

            var products = await ProductHelperService.GetAllProducts(_unitOfWork, sortId, filterId);


            var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
            var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();

            #region Mapping
            var result = products.Select(product => new ProductDto {
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category.Name ?? "UnCategorized",
                Stock = inventories.FirstOrDefault(i => i.ProductId == product.Id)?.Quantity ?? 0,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,

            });
            #endregion
                

            return result;
        }

        public async Task<int> CreateProductAsync(ProductDto productDto) {

            #region Category Retrieval and Assigning
            var category = await _unitOfWork.GetRepository<Category>().GetAsync(c => c.Name == productDto.Category);

            if (category == null) {
                category = new Category { Name = productDto.Category ?? "null" };
                await _unitOfWork.GetRepository<Category>().AddAsync(category);
                await _unitOfWork.SaveChangesAsync();
            }
            #endregion

            // Hardcoded companyId
            #region Mapping and Adding
            var product = new Product {
                Name = productDto.Name,
                SKU = productDto.SKU,
                Category = category,
                CostPrice = productDto.CostPrice,
                SellingPrice = productDto.SellingPrice,
                ExpiryDate = productDto.ExpiryDate,
                Barcode = productDto.Barcode,
                Description = productDto.Description,
                ReorderLevel = productDto.ReorderLevel,
                Status = productDto.Status,
                CompanyId = 3, // hardcoded companyId
            };

            product.Inventories = new List<Inventory> {
                new Inventory {
                    Quantity = productDto.Stock,
                    BranchId = 12,
                }
            };
            #endregion

            await _unitOfWork.GetRepository<Product>().AddAsync(product);
            var count = await _unitOfWork.SaveChangesAsync();

            return product.Id;
        }

        public async Task<int> UpdateProductAsync(int id, ProductDto productDto) {

            var branchId = 1; // hardcoded branchId
            var product = await _unitOfWork.GetRepository<Product>().GetByIdAsync(id);
            var inventory = await _unitOfWork.GetRepository<Inventory>().GetAsync(i => i.ProductId == id && branchId == 1);

            if (product is null || inventory is null) {
                throw new Exception("Product not found");
            }

            #region Mapping

            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.SKU = productDto.SKU;
            inventory.Quantity = productDto.Stock;
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

                var newCategory = new Category { Name = productDto.Category ?? "null" };
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

        // Hard delete logic (Deleting inventory record before product record)
        public async Task<bool> DeletedProductAsync(int id) {

            var branchId = 1; // hardcoded branchId
            var product = await _unitOfWork.GetRepository<Product>().GetByIdAsync(id);
            var inventory = await _unitOfWork.GetRepository<Inventory>().GetAsync(i => i.ProductId == id && branchId == 1);


            if (product is null || inventory is null) {
                throw new Exception("Product not found");
            }

            _unitOfWork.GetRepository<Inventory>().DeleteAsync(inventory);
            _unitOfWork.GetRepository<Product>().DeleteAsync(product);

            var count = await _unitOfWork.SaveChangesAsync();
            return count > 0;
        }

        public Task<int> CsvUploadAsync(string filePath) {
            throw new NotImplementedException();
        }

    }

}

#region Delete if code works
// Create product
//var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();
//var category = categories.FirstOrDefault(c => c.Name == productDto.Category);

// Update product / Delete product
//var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
//var inventory = inventories.FirstOrDefault(i => i.ProductId == id);


#endregion