using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Services.Abstractions.Transactions;
using Tanzeem.Shared.Dtos.Orders;
using Tanzeem.Shared.Dtos.Products;
using Tanzeem.Shared.Dtos.Transactions;

namespace Tanzeem.Services.Transactions {
    public class TransactionService(IUnitOfWork _unitOfWork,
        ICurrentService currentService,
        INotificationService _notificationService)
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
                Type = transaction.Type,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                Value = transaction.Value,
                TotalTransactedItems = transaction.TotalTransactedItems,
                SourceReason = transaction.SourceReason,
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                TransactionItemDtos = transactionItemDtosList,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            };

            #endregion

            return result;
        }

        // Hard coded function (category, stock, performedby, batchnumber)
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
                Type = transaction.Type,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                Value = transaction.Value,
                TotalTransactedItems = transaction.TotalTransactedItems,
                SourceReason = transaction.SourceReason,
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                TransactionItemDtos = transactionItemDtosList,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            });

            #endregion

            return result;

        }





        // Hard coded branchId / UserId for now, will be taken from the current service in the future.
        // Handle Global Exception in Controller/Middleware, not here. This function should just throw and let the caller handle it.
        public async Task<int> CreateTransactionAsync(TransactionDto transactionDto) {
            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try {

                #region Entities Loading

                // Load inventories for the branch
                var inventories = await _unitOfWork.GetRepository<Inventory>()
                    .GetAllAsIQueryable()
                    .AsTracking()
                    .Include(x => x.Product)
                    .Where(x => x.BranchId == 1) // currentService.BranchId || hardcoded for now
                    .ToListAsync();

                // Load all needed products in one shot
                var skus = transactionDto.TransactionItemDtos.Select(x => x.Product.SKU).ToHashSet();
                var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();
                var productsBySku = products
                    .Where(p => skus.Contains(p.SKU))
                    .ToDictionary(p => p.SKU);

                #endregion

                #region Mapping

                // Map TransactionItems
                var transactionItems = transactionDto.TransactionItemDtos.Select(item => {
                    if (!productsBySku.TryGetValue(item.Product.SKU, out var product))
                        throw new Exception($"Product with SKU '{item.Product.SKU}' not found.");

                    return new TransactionItem {
                        QuantityOfTransactedItem = item.QuantityOfTransactedItem,
                        UnitPrice = item.UnitPrice,
                        BatchNumber = item.BatchNumber,
                        Product = product,
                    };
                }).ToList();

                // Map Transaction
                var transaction = new Transaction {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = transactionDto.Type,
                    CreatedAt = transactionDto.CreatedAt,
                    Status = transactionDto.Status,
                    Value = transactionItems.Sum(x => x.UnitPrice * x.QuantityOfTransactedItem),
                    TotalTransactedItems = transactionItems.Sum(x => x.QuantityOfTransactedItem),
                    SourceReason = transactionDto.SourceReason,
                    ReferenceNumber = transactionDto.ReferenceNumber,
                    Notes = transactionDto.Notes,
                    TransactionItems = transactionItems,
                    BranchId = 1, // currentService.BranchId ?? throw new Exception("Branch not found")  
                    PerformedByUserId =  3 // hard coded User Id
                };

                #endregion

                #region Update inventory quantities

                if (transactionDto.Type == TransactionType.In)
                    InTransaction(transactionItems, inventories);
                else if (transactionDto.Type == TransactionType.Out)
                    OutTransaction(transactionItems, inventories);

                #endregion


                await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
                await _unitOfWork.SaveChangesAsync();

                await dbTransaction.CommitAsync();

                await LowStockAlertAsync(transaction, transactionItems, inventories);

                return transaction.Id;
            }
            catch {
                await dbTransaction.RollbackAsync();
                throw; // let the caller/middleware handle it
            }

        }

        #region In / Out Private Functions
        private void InTransaction(List<TransactionItem> transactionItems, List<Inventory> inventories) {
            foreach (var item in transactionItems) {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.Product.Id);

                if (inventory == null)
                    throw new Exception($"Inventory record not found for product '{item.Product.Name}' (SKU: {item.Product.SKU}).");

                inventory.Quantity = (inventory.Quantity ?? 0) + item.QuantityOfTransactedItem;
            }
        }

        private void OutTransaction(List<TransactionItem> transactionItems, List<Inventory> inventories) {
            foreach (var item in transactionItems) {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.Product.Id);

                if (inventory == null)
                    throw new Exception($"Inventory record not found for product '{item.Product.Name}' (SKU: {item.Product.SKU}).");

                if ((inventory.Quantity ?? 0) < item.QuantityOfTransactedItem)
                    throw new Exception($"Insufficient stock for product '{item.Product.Name}'. Available: {inventory.Quantity}, Requested: {item.QuantityOfTransactedItem}.");

                inventory.Quantity -= item.QuantityOfTransactedItem;
            }
        }

        #endregion

        // branchId is hard coded here
        #region Old Low Stock Alert
        /* 
        private async void LowStockAlert(Transaction transaction, List<TransactionItem> transactionItems, List<Inventory> inventories) {

            if (transaction.Type == TransactionType.Out) {
                var lowStockItems = transactionItems.Where(item => {
                    var inventory = inventories.FirstOrDefault(x => x.ProductId == item.ProductId && x.BranchId == 1); // branchId is hard coded here
                    if (inventory == null) {
                        throw new Exception("this inventory not found");
                        ///TODO exception handling
                    }
                    return inventory.Quantity <= inventory.Product.ReorderLevel;
                }).ToList();

                if (lowStockItems.Any()) {
                    await _notificationService.CreateLowStockNotification(lowStockItems, inventories);
                }

            }

        }
        */
        #endregion
        private async Task LowStockAlertAsync(Transaction transaction, List<TransactionItem> transactionItems, List<Inventory> inventories) {
            try {
                if (transaction.Type != TransactionType.Out) return;

                var lowStockItems = transactionItems.Where(item => {
                    var inventory = inventories.FirstOrDefault(x => x.ProductId == item.Product.Id && x.BranchId == 1);
                    return inventory != null && inventory.Quantity <= inventory.Product.ReorderLevel;
                }).ToList();

                if (lowStockItems.Any())
                    await _notificationService.CreateLowStockNotification(lowStockItems, inventories);
            }
            catch (Exception ex) {
                // Don't let a failed alert bubble up and affect the transaction
                // TODO: plug into your logger here → _logger.LogError(ex, "LowStockAlert failed")
            }
        }


        public async Task<int> CreateConfirmOrderTransactionAsync(Order order) {
            if (order is null) {
                return 0; ///TODO exceptionhandling
            }

            var transactionsItems = order.Items.Select(orderItem => new TransactionItem() {
                Product = orderItem.Product,
                QuantityOfTransactedItem = orderItem.Quantity,
                UnitPrice = orderItem.Price,
                ProductId = orderItem.ProductId,
            }).ToList();

            Transaction transaction = new Transaction() {
                BranchId = 1, ///TODO auth

                TransactionId = Guid.NewGuid().ToString(),

                CreatedAt = (DateTime)order?.RecievedDeliveryDate!,

                SourceReason = TransactionSource.Supplier,

                TransactionItems = transactionsItems,

                TotalTransactedItems = transactionsItems.Sum(item => item.QuantityOfTransactedItem),

                Type = TransactionType.In,

                Value = transactionsItems.Sum(item => item.UnitPrice * item.QuantityOfTransactedItem),

                Notes = order.Notes ?? "Order confirmed",

                ReferenceNumber = "--",
            };

            await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            return transaction.Id;
        }

    }
}
