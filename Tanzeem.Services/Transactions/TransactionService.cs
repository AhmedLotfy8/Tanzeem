using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Shared.Dtos.Products;
using Tanzeem.Shared.Dtos.Transactions;

namespace Tanzeem.Services.Transactions {
    public class TransactionService(IUnitOfWork _unitOfWork)
        : ITransactionService {

        public async Task<TransactionDto> GetTransactionByIdAsync(int id) {

            var transaction = await _unitOfWork.GetRepository<Transaction>().GetByIdAsync(id);

            if (transaction == null) {
                throw new KeyNotFoundException($"Transaction with ID {id} not found.");
            }

            #region Mapping

            var transactionItemsList = _unitOfWork.GetRepository<TransactionItem>()
                .GetAllAsync().Result.Where(x => x.TransactionId == transaction.Id).ToList();

            var transactionItemDtosList = new List<TransactionItemDto>();

            foreach (var item in transactionItemsList) {

                var product = _unitOfWork.GetRepository<Product>().GetAllAsync()
                            .Result.FirstOrDefault(x => x.SKU == item.Product.SKU)
                              ?? throw new Exception("Product not found");

                // Mapping TransactionItem to TransactionItemDto
                var transactionItemDto = new TransactionItemDto {

                    QuantityOfTransactedItem = item.QuantityOfTransactedItem,
                    UnitPrice = item.UnitPrice,

                    // Mapping Product to ProductDto
                    Product = new ProductDto {
                        Name = product.Name,
                        SKU = product.SKU,
                        Category = "tempCat",
                        Stock = 987123546,
                        CostPrice = product.CostPrice,
                        SellingPrice = product.SellingPrice,
                        ExpiryDate = product.ExpiryDate,
                        Barcode = product.Barcode,
                        Description = product.Description,
                        ReorderLevel = product.ReorderLevel,
                        Status = product.Status,
                    }
                };

                transactionItemDtosList.Add(transactionItemDto);
            }

            // Mapping Transaction to TransactionDto
            var result = new TransactionDto {
                Id = transaction.TransactionId,
                Type = transaction.Type.ToString(),
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status.ToString(),
                Value = transaction.Value,
                TotalTransactedItems = transaction.TotalTransactedItems,
                SourceReason = transaction.SourceReason.ToString(),
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                TransactionItemDtos = transactionItemDtosList,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            };

            #endregion

            return result;
        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactions(int? filterId, int? sortId) {

            var transactions = await TransactionHelperService.GetAllTransactions(_unitOfWork, sortId, filterId);

            #region Mapping

            var transactionItemDtosList = new List<TransactionItemDto>();

            // Mapping TransactionItems to TransactionItemDtos
            foreach (var item in transactions) {

                var transactionItemsList = _unitOfWork.GetRepository<TransactionItem>()
                    .GetAllAsync().Result.Where(x => x.TransactionId == item.Id).ToList();


                // Mapping TransactionItems to TransactionItemDtos
                foreach (var ti in transactionItemsList) {

                    var product = _unitOfWork.GetRepository<Product>().GetAllAsync()
                            .Result.FirstOrDefault(x => x.SKU == ti.Product.SKU)
                              ?? throw new Exception("Product not found");


                    // Mapping TransactionItem to TransactionItemDto
                    var transactionItemDto = new TransactionItemDto {

                        QuantityOfTransactedItem = ti.QuantityOfTransactedItem,
                        UnitPrice = ti.UnitPrice,

                        // Mapping Product to ProductDto
                        Product = new ProductDto {
                            Name = product.Name,
                            SKU = product.SKU,
                            Category = "TempCat",
                            Stock = 987123546,
                            CostPrice = product.CostPrice,
                            SellingPrice = product.SellingPrice,
                            ExpiryDate = product.ExpiryDate,
                            Barcode = product.Barcode,
                            Description = product.Description,
                            ReorderLevel = product.ReorderLevel,
                            Status = product.Status,
                        }

                    };


                    transactionItemDtosList.Add(transactionItemDto);
                }

            }

            // Mapping Transactions to TransactionDtos
            var result = transactions.Select(transaction => new TransactionDto {
                Id = transaction.TransactionId,
                Type = transaction.Type.ToString(),
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status.ToString(),
                Value = transaction.Value,
                TotalTransactedItems = transaction.TotalTransactedItems,
                SourceReason = transaction.SourceReason.ToString(),
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                TransactionItemDtos = transactionItemDtosList,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            });

            #endregion

            return result;

        }

        public async Task<int> CreateTransactionAsync(TransactionDto transactionDto) {

            #region Mapping

            // Mapping TransactionItemDtos to TransactionItems
            var transactionItems = transactionDto.TransactionItemDtos.Select(item =>

            // Mapping TransactionItemDto to TransactionItem
            new TransactionItem {
                QuantityOfTransactedItem = item.QuantityOfTransactedItem,
                UnitPrice = item.UnitPrice,

                // Retriving product
                Product = _unitOfWork.GetRepository<Product>().GetAllAsync()
                            .Result.FirstOrDefault(x => x.SKU == item.Product.SKU)
                              ?? throw new Exception("Product not found"),

            }).ToList();


            // Mapping TransactionDto to Transaction
            var transaction = new Transaction {
                TransactionId = Guid.NewGuid().ToString(),
                Type = Enum.Parse<TransactionType>(transactionDto.Type),
                CreatedAt = transactionDto.CreatedAt,
                Status = Enum.Parse<TransactionStatus>(transactionDto.Status),
                Value = transactionDto.Value,
                TotalTransactedItems = transactionDto.TotalTransactedItems,
                SourceReason = Enum.Parse<TransactionSource>(transactionDto.SourceReason),
                ReferenceNumber = transactionDto.ReferenceNumber,
                Notes = transactionDto.Notes,
                TransactionItems = transactionItems,
                BranchId = 1 // dummy value
            };

            #endregion

            await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
            var count = await _unitOfWork.SaveChangesAsync();

            #region low stock alert
            if (transaction.Type == TransactionType.Out)
            {
                

            }
            #endregion

            return transaction.Id;
        }

    }
}
