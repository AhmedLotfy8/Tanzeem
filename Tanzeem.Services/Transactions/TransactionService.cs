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
                Type = transaction.Type.ToString(),
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status.ToString(),
                Value = transaction.Value,
                Quantity = transaction.Quantity,
                SourceReason = transaction.SourceReason.ToString(),
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            };
            #endregion

            return result;

        }

        public async Task<IEnumerable<TransactionDto>> GetAllTransactions(int? filterId, int? sortId) {
            
            var transactions = await TransactionHelperService.GetAllTransactions(_unitOfWork, sortId, filterId);

            var result = transactions.Select(transaction => new TransactionDto {
                Id = transaction.TransactionId,
                Type = transaction.Type.ToString(),
                CreatedAt = transaction.CreatedAt,
                Status = transaction.Status.ToString(),
                Value = transaction.Value,
                Quantity = transaction.Quantity,
                SourceReason = transaction.SourceReason.ToString(),
                ReferenceNumber = transaction.ReferenceNumber,
                Notes = transaction.Notes,
                PreformedBy = "User", // dummy value
                BatchNumber = "BatchNumber" // dummy value
            });

            return result;

        }

        public async Task<int> CreateTransactionAsync(TransactionDto transactionDto) {

            #region Mapping
            var transaction = new Transaction {
                TransactionId = Guid.NewGuid().ToString(),
                Type = Enum.Parse<TransactionType>(transactionDto.Type),
                CreatedAt = transactionDto.CreatedAt,
                Status = Enum.Parse<TransactionStatus>(transactionDto.Status),
                Value = transactionDto.Value,
                Quantity = transactionDto.Quantity,
                SourceReason = Enum.Parse<TransactionSource>(transactionDto.SourceReason),
                ReferenceNumber = transactionDto.ReferenceNumber,
                Notes = transactionDto.Notes,
                BranchId = 1 // dummy value
            };
            #endregion

            await _unitOfWork.GetRepository<Transaction>().AddAsync(transaction);
            var count = await _unitOfWork.SaveChangesAsync();

            return transaction.Id;
        }

    }
}
