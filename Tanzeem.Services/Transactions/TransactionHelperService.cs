using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Transactions;
using Tanzeem.Domain.Enums;

namespace Tanzeem.Services.Transactions {
    public static class TransactionHelperService {

        public static async Task<IEnumerable<Transaction>> GetAllTransactions(IUnitOfWork _unitOfWork, int? sortId, int? filterId) {

            var transactions = await _unitOfWork.GetRepository<Transaction>().GetAllAsync();
            
            #region Cases (Is sorted / Is filtered)

            if (sortId.HasValue && filterId.HasValue) {
                var filteredProducts = FilterTransactions(_unitOfWork, transactions, filterId);
                return SortTransactions(_unitOfWork, filteredProducts, sortId);
            }

            else if (filterId.HasValue)
                return FilterTransactions(_unitOfWork, transactions, filterId);

            else if (sortId.HasValue)
                return SortTransactions(_unitOfWork, transactions, sortId);

            #endregion

            return transactions;

        }

        private static IEnumerable<Transaction> SortTransactions(IUnitOfWork _unitOfWork,
            IEnumerable<Transaction> transactions, int? sortId) {
            
            switch (sortId) {
                
                case 1:
                    return transactions.OrderBy(t => t.CreatedAt).ToList();
                
                case 2:
                    return transactions.OrderBy(t => t.Value).ToList();
                
                case 3:
                    return transactions.OrderBy(t => t.TotalTransactedItems).ToList();
                
                case null:
                    return transactions.OrderBy(t => t.Id).ToList();
                
                default:
                    throw new Exception("Invalid sort option");
            }

        }

        private static IEnumerable<Transaction> FilterTransactions(IUnitOfWork _unitOfWork,
            IEnumerable<Transaction> transactions, int? filterId) {

            switch (filterId) {

                case int filter when (filter >= 1 && filter <= 3):
                    return transactions.Where(t => t.Type == (TransactionType)filter).ToList();
                
                case int filter when (filter >= 4 && filterId <= 6):
                    return transactions.Where(t => t.Status == (TransactionStatus)filter).ToList();
                
                case int filter when (filter >= 7 && filter <= 12):
                    return transactions.Where(t => t.SourceReason == (TransactionSource)filter).ToList();

                case null:
                    return transactions.ToList();
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(filterId), "Invalid filter option");

            }

        }

    }

}

#region To be Done Later (but for transactions)
//public static async Task<IEnumerable<Product>> SearchProducts(string searchTerm, IUnitOfWork _unitOfWork) {
//    var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();
//    return products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
//}
#endregion