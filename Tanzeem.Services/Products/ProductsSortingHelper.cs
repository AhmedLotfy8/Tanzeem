using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Products;

namespace Tanzeem.Services.Products {
    public static class ProductsSortingHelper {

        public static async Task<IEnumerable<Product>> SortProducts(int? sortId, IUnitOfWork _unitOfWork) {

            switch (sortId) {
                case 1:
                    return await SortProductsByName(_unitOfWork);
                case 2:
                    return await SortProductsByPrice(_unitOfWork);
                case 3:
                    return await SortProductsByStock(_unitOfWork);
                case null:
                    return await DefaultSortById(_unitOfWork);
                default:
                    throw new Exception("Invalid sort option");
            }

        }


        private static async Task<IEnumerable<Product>> DefaultSortById(IUnitOfWork _unitOfWork) {

            var products = await _unitOfWork.GetRepository<Product>()
                .GetAllAsync();

            return products.OrderBy(p => p.Id).ToList();
        }

        private static async Task<IEnumerable<Product>> SortProductsByName(IUnitOfWork _unitOfWork) {

            var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();

            return products.OrderBy(p => p.Name).ToList();
        }

        private static async Task<IEnumerable<Product>> SortProductsByPrice(IUnitOfWork _unitOfWork) {

            var products = await _unitOfWork.GetRepository<Product>()
                .GetAllAsync();

            return products.OrderBy(p => p.SellingPrice).ToList();
        }

        private static async Task<IEnumerable<Product>> SortProductsByStock(IUnitOfWork _unitOfWork) {

            var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();
            var inventories = await _unitOfWork.GetRepository<Inventory>().GetAllAsync();

            return products.OrderBy(p => inventories
                .FirstOrDefault(i => i.BranchId == 1 && i.ProductId == p.Id)?.Quantity ?? 0).ToList();

        } 

    }
}
