using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.AIDemand;
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

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            // Single query — loads Category via Include, scoped to company
            var product = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                throw new Exception("Product not found");

            // Branch-scoped inventory
            var inventory = await _unitOfWork.GetRepository<Inventory>()
                .GetAsync(i => i.ProductId == id && i.BranchId == branchId);

            if (inventory is null)
                throw new Exception("Inventory not found for this branch");
            
            #endregion

            return new ProductDto {
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category?.Name ?? "-",
                Stock = inventory.Quantity ?? 0,
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,
            };
        }

        public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(int? sortId, int? filterId) {

            var products = await productHelperService.GetAllProducts(sortId, filterId);

            return products.Select(product => new ProductDto {
                Name = product.Name,
                SKU = product.SKU,
                Category = product.Category?.Name ?? "Uncategorized",
                CostPrice = product.CostPrice,
                SellingPrice = product.SellingPrice,
                ExpiryDate = product.ExpiryDate,
                Barcode = product.Barcode,
                Description = product.Description,
                ReorderLevel = product.ReorderLevel,
                Status = product.Status,
                Stock = product.Inventories
                                      .Where(i => i.BranchId == currentService.BranchId)
                                      .Select(i => i.Quantity)
                                      .FirstOrDefault()
            });
        }

        public async Task<IEnumerable<ProductDropdownMenuDto>> GetAllProductsMenuAsync() {

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            return await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Where(p => p.CompanyId == companyId)
                .Select(p => new ProductDropdownMenuDto {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    Price = p.SellingPrice
                })
                .Take(15)
                .ToListAsync();
        }

        public async Task<int> CreateProductAsync(ProductDto productDto) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            #endregion

            #region Product already registered to this company

            var existingProduct = await _unitOfWork.GetRepository<Product>()
                .GetAsync(p => p.SKU == productDto.SKU && p.CompanyId == companyId);

            if (existingProduct is not null) {

                var isFoundInInventory = await _unitOfWork.GetRepository<Inventory>()
                    .GetAsync(i => i.ProductId == existingProduct.Id && i.BranchId == branchId);

                if (isFoundInInventory is not null)
                    throw new Exception("Product with the same SKU already exists in this branch.");

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

            #region New product — not yet registered to this company

            var transc = await _unitOfWork.BeginTransactionAsync();
            try {

                if (string.IsNullOrWhiteSpace(productDto.Category))
                    throw new Exception("Category name cannot be empty");

                var category = await _unitOfWork.GetRepository<Category>()
                    .GetAsync(c => c.Name == productDto.Category);

                if (category is null) {
                    category = new Category { Name = productDto.Category };
                    await _unitOfWork.GetRepository<Category>().AddAsync(category);
                }

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
                    CompanyId = companyId
                };

                AddProductToBranch(branchId, product, productDto.Stock ?? 0);
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
    
        private void AddProductToBranch(int branchId, Product product, int initialQuantity) {
            product.Inventories ??= new List<Inventory>();
            product.Inventories.Add(new Inventory {
                Quantity = initialQuantity,
                BranchId = branchId
            });
        }
    
        public async Task<int> UpdateProductAsync(int id, ProductDto productDto) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var product = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                throw new Exception("Product not found");

            var inventory = await _unitOfWork.GetRepository<Inventory>()
                .GetAsync(i => i.ProductId == id && i.BranchId == branchId);

            if (inventory is null)
                throw new Exception("Inventory not found for this branch");

            #endregion

            #region Update

            // Product fields
            product.Name = productDto.Name;
            product.Description = productDto.Description;
            product.SKU = productDto.SKU;
            product.CostPrice = productDto.CostPrice;
            product.SellingPrice = productDto.SellingPrice;
            product.ExpiryDate = productDto.ExpiryDate;
            product.Barcode = productDto.Barcode;
            product.ReorderLevel = productDto.ReorderLevel;
            product.Status = productDto.Status;

            // Inventory field
            inventory.Quantity = productDto.Stock;

            #endregion

            #region Category name change

            // Category — DB-level lookup, not in-memory
            if (string.IsNullOrWhiteSpace(productDto.Category))
                throw new Exception("Category name cannot be empty");

            var categoryMatch = await _unitOfWork.GetRepository<Category>()
                .GetAsync(c => c.Name == productDto.Category);

            if (categoryMatch is null) {
                var newCategory = new Category { Name = productDto.Category };
                await _unitOfWork.GetRepository<Category>().AddAsync(newCategory);
                product.Category = newCategory;
            }
            else {
                product.Category = categoryMatch;
            }

            #endregion

            _unitOfWork.GetRepository<Product>().UpdateAsync(product);
            await _unitOfWork.SaveChangesAsync();
            return product.Id;
        
        }

        public async Task<bool> DeletedProductAsync(int id) {

            #region Retrieval

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var product = await _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == companyId);

            if (product is null)
                throw new Exception("Product not found");

            var inventory = await _unitOfWork.GetRepository<Inventory>()
                .GetAsync(i => i.ProductId == id && i.BranchId == branchId);

            if (inventory is null)
                throw new Exception("Inventory not found for this branch");
            var forecast = await _unitOfWork.GetRepository<DemandForecast>()
                .GetAsync(d => d.ProductId == id && d.BranchId == branchId);

            if (forecast is null)
                throw new Exception("Forecast not found for this product");

            #endregion

            _unitOfWork.GetRepository<Inventory>().DeleteAsync(inventory);
            _unitOfWork.GetRepository<DemandForecast>().DeleteAsync(forecast);
            _unitOfWork.GetRepository<Product>().DeleteAsync(product);

            var count = await _unitOfWork.SaveChangesAsync();
            return count > 0;
        }

        public Task<int> CsvUploadAsync(string filePath) {
            throw new NotImplementedException();
        }

    }
}