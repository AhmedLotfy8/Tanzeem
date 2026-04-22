using Microsoft.EntityFrameworkCore;
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
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Products {
    public class ProductHelperService(IUnitOfWork _unitOfWork,
        ICurrentService currentService) {

        public async Task<IEnumerable<Product>> GetAllProducts(int? sortId, int? filterId) {
            var query = _unitOfWork.GetRepository<Product>().GetAllAsIQueryable();
            query = query.Include(x => x.Category);
            query = query.Include(x => x.Inventories);
            
            // to be global query filter
            query = query.Where(p => p.CompanyId == currentService.CompanyId);

            // Filter by category
            if (filterId.HasValue)
                query = query.Where(p => p.CategoryId == filterId);

            // Sort
            query = sortId switch {
                1 => query.OrderBy(p => p.Name),
                2 => query.OrderBy(p => p.SellingPrice),
                3 => query.OrderBy(p => p.Inventories
                             .Where(i => i.BranchId == currentService.BranchId)
                             .Select(i => i.Quantity)
                             .FirstOrDefault()),
                null => query.OrderBy(p => p.Id),
                _ => throw new Exception("Invalid sort option")
            };

            return await query.ToListAsync();
        }
    }

}


#region To be Done Later
//public static async Task<IEnumerable<Product>> SearchProducts(string searchTerm, IUnitOfWork _unitOfWork) {
//    var products = await _unitOfWork.GetRepository<Product>().GetAllAsync();
//    return products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
//}
#endregion
