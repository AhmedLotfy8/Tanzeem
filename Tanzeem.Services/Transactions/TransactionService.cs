using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
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
using Tanzeem.Shared.Dtos.Transactions;

namespace Tanzeem.Services.Transactions {
    public class TransactionService(
        IUnitOfWork _unitOfWork,
        ICurrentService currentService,
        TransactionHelperService transactionHelper,
        INotificationService _notificationService)
        : ITransactionService {

        public async Task<TransactionDto> GetTransactionByIdAsync(int id) {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            // Single query — loads TransactionItems and their Products and the PerformedByUser
            var transaction = await _unitOfWork.GetRepository<Transaction>()
                .GetAllAsIQueryable()
                .Include(t => t.TransactionItems)
                    .ThenInclude(ti => ti.Product)
                        .ThenInclude(p => p.Category)
                .Include(t => t.PreformedByUser)
                .FirstOrDefaultAsync(t => t.Id == id && t.BranchId == branchId);

            if (transaction is null)
                throw new KeyNotFoundException($"Transaction with ID {id} not found.");

            return MapToTransactionDto(transaction, branchId);
        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactions(int? filterId, int? sortId, string? searchQuery) {

            var transactions = await transactionHelper.GetAllTransactions(sortId, filterId, searchQuery);

            return transactions.Select(t => MapToTransactionDto(t, currentService.BranchId ?? 0));
        }

        public async Task<int> CreateTransactionAsync(TransactionDto transactionDto) {

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("UserId not found");

            await using var dbTransaction = await _unitOfWork.BeginTransactionAsync();
            try {

                #region Entities Loading

                // Load branch inventories with their products in one query
                var inventories = await _unitOfWork.GetRepository<Inventory>()
                    .GetAllAsIQueryable()
                    .AsTracking()
                    .Include(i => i.Product)
                    .Where(i => i.BranchId == branchId)
                    .ToListAsync();

                // Load all needed products in one shot by SKU
                var skus = transactionDto.TransactionItemDtos
                    .Select(x => x.Product.SKU)
                    .ToHashSet();

                var productsBySku = await _unitOfWork.GetRepository<Product>()
                    .GetAllAsIQueryable()
                    .AsTracking()          // ensure tracked instances
                    .Where(p => skus.Contains(p.SKU))
                    .ToDictionaryAsync(p => p.SKU);

                #endregion

                #region Mapping

                var transactionItems = transactionDto.TransactionItemDtos.Select(item => {
                    if (!productsBySku.TryGetValue(item.Product.SKU, out var product))
                        throw new KeyNotFoundException($"Product with SKU '{item.Product.SKU}' not found.");

                    return new TransactionItem {
                        QuantityOfTransactedItem = item.QuantityOfTransactedItem,
                        UnitPrice = item.UnitPrice,
                        BatchNumber = item.BatchNumber,
                        Product = product,
                    };
                }).ToList();

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
                    BranchId = branchId,
                    PerformedByUserId = userId
                };

                #endregion

                #region Update Inventory

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
                throw;
            }
        }

        public async Task<int> CreateConfirmOrderTransactionAsync(Order order) {

            if (order is null)
                throw new ArgumentNullException(nameof(order), "Order cannot be null");

            var branchId = currentService.BranchId
                ?? throw new UnauthorizedAccessException("BranchId not found");

            var userId = currentService.UserId
                ?? throw new UnauthorizedAccessException("UserId not found");

            var receivedDate = order.RecievedDeliveryDate
                ?? throw new InvalidOperationException("Order delivery date is required to confirm a transaction");

            var transactionItems = order.Items.Select(orderItem => new TransactionItem {
                Product = orderItem.Product,
                QuantityOfTransactedItem = orderItem.Quantity,
                UnitPrice = orderItem.Price,
                ProductId = orderItem.ProductId,
                BatchNumber = string.Empty
            }).ToList();

            var transaction = new Transaction {
                BranchId = branchId,
                PerformedByUserId = userId,
                TransactionId = Guid.NewGuid().ToString(),
                CreatedAt = receivedDate,
                SourceReason = TransactionSource.Supplier,
                TransactionItems = transactionItems,
                TotalTransactedItems = transactionItems.Sum(item => item.QuantityOfTransactedItem),
                Type = TransactionType.In,
                Status = TransactionStatus.Completed,
                Value = transactionItems.Sum(item => item.UnitPrice * item.QuantityOfTransactedItem),
                Notes = order.Notes ?? "Order confirmed",
                ReferenceNumber = "--",
            };

            await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
            await _unitOfWork.SaveChangesAsync();

            return transaction.Id;
        }

        #region Private Helpers

        private TransactionDto MapToTransactionDto(Transaction transaction, int branchId) {
            var itemDtos = transaction.TransactionItems.Select(ti => new TransactionItemDto {
                QuantityOfTransactedItem = ti.QuantityOfTransactedItem,
                UnitPrice = ti.UnitPrice,
                BatchNumber = ti.BatchNumber ?? string.Empty,
                Product = new Shared.Dtos.Products.ProductDto {
                    Name = ti.Product.Name,
                    SKU = ti.Product.SKU,
                    Category = ti.Product.Category?.Name ?? "Uncategorized",
                    CostPrice = ti.Product.CostPrice,
                    SellingPrice = ti.Product.SellingPrice,
                    ExpiryDate = ti.Product.ExpiryDate,
                    Barcode = ti.Product.Barcode,
                    Description = ti.Product.Description,
                    ReorderLevel = ti.Product.ReorderLevel,
                    Status = ti.Product.Status,
                    Stock = ti.Product.Inventories
                                     .Where(i => i.BranchId == branchId)
                                     .Select(i => i.Quantity)
                                     .FirstOrDefault()
                }
            }).ToList();

            return new TransactionDto {
                Id = transaction.Id,
                TransactionId = transaction.TransactionId,
                Type = transaction.Type,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                Value = transaction.Value,
                TotalTransactedItems = transaction.TotalTransactedItems,
                SourceReason = transaction.SourceReason,
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                PreformedBy = transaction.PreformedByUser?.Name ?? "-",
                TransactionItemDtos = itemDtos
            };
        }

        private void InTransaction(List<TransactionItem> transactionItems, List<Inventory> inventories) {
            foreach (var item in transactionItems) {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.Product.Id)
                    ?? throw new KeyNotFoundException(
                        $"Inventory record not found for product '{item.Product.Name}' (SKU: {item.Product.SKU}).");

                inventory.Quantity = (inventory.Quantity ?? 0) + item.QuantityOfTransactedItem;
            }
        }

        private void OutTransaction(List<TransactionItem> transactionItems, List<Inventory> inventories) {
            foreach (var item in transactionItems) {
                var inventory = inventories.FirstOrDefault(inv => inv.ProductId == item.Product.Id)
                    ?? throw new KeyNotFoundException(
                        $"Inventory record not found for product '{item.Product.Name}' (SKU: {item.Product.SKU}).");

                if ((inventory.Quantity ?? 0) < item.QuantityOfTransactedItem)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{item.Product.Name}'. Available: {inventory.Quantity}, Requested: {item.QuantityOfTransactedItem}.");

                inventory.Quantity -= item.QuantityOfTransactedItem;
            }
        }

        private async Task LowStockAlertAsync(
            Transaction transaction,
            List<TransactionItem> transactionItems,
            List<Inventory> inventories) {
            try {
                if (transaction.Type != TransactionType.Out) return;

                // inventories is already branch-scoped from CreateTransactionAsync — match by ProductId only
                var lowStockItems = transactionItems
                    .Where(item => {
                        var inventory = inventories.FirstOrDefault(x => x.ProductId == item.Product.Id);
                        return inventory != null && inventory.Quantity <= inventory.Product.ReorderLevel;
                    })
                    .ToList();

                if (lowStockItems.Any())
                    await _notificationService.CreateLowStockNotification(lowStockItems, inventories);
            }
            catch {
                // Never let a failed alert bubble up and roll back the transaction
                // TODO: plug into your logger here → _logger.LogError(ex, "LowStockAlert failed for TransactionId: {id}", transaction.Id)
            }
        }

        #endregion
    
    }
}

