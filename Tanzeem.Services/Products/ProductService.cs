using Microsoft.AspNetCore.Http;
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
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Products;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Products {
    public class ProductService(IUnitOfWork _unitOfWork,
        ProductHelperService productHelperService,
        ICurrentService currentService) : IProductService {

        public async Task<ProductDto> GetProductByIdAsync(int id) {

            var product = await _unitOfWork.GetRepository<Product>().GetByIdAsync(id);

            var inventory = await _unitOfWork.GetRepository<Inventory>().GetAsync(i => i.ProductId == id);
            var categoryName = await _unitOfWork.GetRepository<Category>().GetAsync(c => c.Name == product.Category.Name);

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
            var products = await productHelperService.GetAllProducts(sortId, filterId);

            #region Mapping

            var result = products.Select(product => new ProductDto {
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category?.Name ?? "UnCategorized",
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,
                Stock = product.Inventories
                               .Where(i => i.BranchId == currentService.BranchId!)
                               .Select(i => i.Quantity)
                               .FirstOrDefault()
            });

            #endregion

            return result;
        }

        public async Task<int> CreateProductAsync(ProductDto productDto) {

            #region If Product is registered to the company

            var existingProduct = await _unitOfWork.GetRepository<Product>().GetAsync(p => p.SKU == productDto.SKU);
            if (existingProduct is not null) {

                var branchId = currentService.BranchId ?? throw new UnauthorizedAccessException("BranchId not found");
                var isFoundInInventory = await _unitOfWork.GetRepository<Inventory>()
                    .GetAsync(i => i.ProductId == existingProduct.Id && i.BranchId == branchId);
                if (isFoundInInventory is not null) {
                    throw new Exception("Product with the same SKU already exists! Cannot recreate.");
                }

                var tranac = await _unitOfWork.BeginTransactionAsync();
                try {
                    AddProductToBranch(branchId, existingProduct, productDto.Stock ?? 0);
                    await _unitOfWork.SaveChangesAsync();
                    await tranac.CommitAsync();
                    return existingProduct.Id;
                }
                catch {
                    await tranac.RollbackAsync();
                    throw;
                }

            }

            #endregion

            #region If Product is Not registered to the company

            var transc = await _unitOfWork.BeginTransactionAsync();
            try {

                #region Category Assigning

                var category = await _unitOfWork.GetRepository<Category>()
                    .GetAsync(c => c.Name == productDto.Category);
                if (category is null) {

                    if (string.IsNullOrWhiteSpace(productDto.Category)) {
                        throw new Exception("Category name cannot be empty");
                    }

                    category = new Category { Name = productDto.Category };
                    await _unitOfWork.GetRepository<Category>().AddAsync(category);
                }

                #endregion

                #region Mapping

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
                    CompanyId = currentService.CompanyId ?? throw new UnauthorizedAccessException("CompanyId not found")
                };


                #endregion

                AddProductToBranch((int)currentService.BranchId!, product, productDto.Stock ?? 0);
                await _unitOfWork.GetRepository<Product>().AddAsync(product);
                await _unitOfWork.SaveChangesAsync();

                await transc.CommitAsync();
                return product.Id;
            }

            catch {
                await transc.RollbackAsync();
                throw;
            }

            #endregion

        }

        // Hard coded function
        public async Task<int> UpdateProductAsync(int id, ProductDto productDto) {

            var branchId = 1;
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

        // Hard coded function / delete logic (Deleting inventory record before product record)
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

        private void AddProductToBranch(int branchId, Product product, int initialQuantity) {
            product.Inventories ??= new List<Inventory>();
            product.Inventories.Add(new Inventory {
                Quantity = initialQuantity,
                BranchId = branchId
            });
        }

    }

}

#region Delete if code works
//get by id
//var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
//var inventory = inventories.FirstOrDefault(i => i.ProductId == id);
//var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();


// Create product
//var categories = await _unitOfWork.GetRepository<Category>().GetAllAsync();
//var category = categories.FirstOrDefault(c => c.Name == productDto.Category);

// Update product / Delete product
//var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
//var inventory = inventories.FirstOrDefault(i => i.ProductId == id);


#endregion