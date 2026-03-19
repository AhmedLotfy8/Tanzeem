using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Services.Abstractions.Transactions;
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
            var result = new TransactionDto {
                Id = transaction.TransactionId,
                Type = transaction.Type,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                Value = transaction.Value,
                Quantity = transaction.Quantity,
                SourceReason = transaction.SourceReason,
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            };
            #endregion

            return result;

        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactions() {
            
            var transactions = await _unitOfWork.GetRepository<Transaction>().GetAllAsync();

            var result = transactions.Select(transaction => new TransactionDto {
                Id = transaction.TransactionId,
                Type = transaction.Type,
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status,
                Value = transaction.Value,
                Quantity = transaction.Quantity,
                SourceReason = transaction.SourceReason,
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            });

            return result;

        }

        public async Task<int> CreateTransactionAsync(TransactionDto transactionDto) {
            
            var transaction = new Transaction {
                TransactionId = transactionDto.Id,
                Type = transactionDto.Type,
                CreatedAt = transactionDto.CreatedAt,
                Status = transactionDto.Status,
                Value = transactionDto.Value,
                Quantity = transactionDto.Quantity,
                SourceReason = transactionDto.SourceReason,
                ReferenceNumber = transactionDto.ReferenceNumber,
                Notes = transactionDto.Notes,
                BranchId = 1 // dummy value
            };

            await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
            var count = await _unitOfWork.SaveChangesAsync();

            return transaction.Id;
        }

    }
}
