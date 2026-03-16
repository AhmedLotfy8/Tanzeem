using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Services.Products {
    public static class ProductHelperService {

        public static async Task<IEnumerable<Product>> GetAllProducts(IUnitOfWork _unitOfWork, int? sortId, int? filterId) {
            
            var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();


            #region Cases (Is sorted / Is filtered)
            if (sortId.HasValue && filterId.HasValue) {
                var filteredProducts = FilterProducts(_unitOfWork, products, filterId);
                return await SortProducts(_unitOfWork, filteredProducts, sortId);
            }

            else if (filterId.HasValue)
                return FilterProducts(_unitOfWork, products, filterId);
            
            else if (sortId.HasValue)
                return await SortProducts(_unitOfWork, products, sortId);


            return await _unitOfWork.GetRepository<Product>().GetAllAsync();
            #endregion

        }

        private static async Task<IEnumerable<Product>> SortProducts(IUnitOfWork _unitOfWork,
            IEnumerable<Product> products, int? sortId) {

            switch (sortId) {

                case 1:
                    return products.OrderBy(p => p.Name).ToList();

                case 2:
                    return products.OrderBy(p => p.SellingPrice).ToList();

                case 3: // hard coded branch id for now, will use currentBranchId in the future
                    var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();
                    return products.OrderBy(p => inventories.FirstOrDefault(i => i.BranchId == 1 && i.ProductId == p.Id)?.Quantity ?? 0).ToList();

                case null:
                    return products.OrderBy(p => p.Id).ToList();

                default:
                    throw new Exception("Invalid sort option");

            }

        }

        private static IEnumerable<Product> FilterProducts(IUnitOfWork _unitOfWork,IEnumerable<Product> products, int? filterId) {

            return products.Where(p => p.CategoryId == filterId).ToList();
        }

    }
}

#region To be Done Later
//public static async Task<IEnumerable<Product>> SearchProducts(string searchTerm, IUnitOfWork _unitOfWork) {
//    var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();
//    return products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
//}
#endregion
