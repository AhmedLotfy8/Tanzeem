using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Services.Abstractions.Current;

namespace Tanzeem.Services.Products {
    public class ProductHelperService(IUnitOfWork _unitOfWork,
        ICurrentService currentService) {

        public async Task<IEnumerable<Product>> GetAllProducts(int? sortId, int? filterId) {

            var companyId = currentService.CompanyId
                ?? throw new UnauthorizedAccessException("CompanyId not found");

            // Declare as IQueryable<Product> first, apply filters before includes
            IQueryable<Product> query = _unitOfWork.GetRepository<Product>()
                .GetAllAsIQueryable()
                .Where(p => p.CompanyId == companyId);

            if (filterId.HasValue)
                query = query.Where(p => p.CategoryId == filterId);

            query = sortId switch {
                1 => query.OrderBy(p => p.Name),
                2 => query.OrderBy(p => p.SellingPrice),
                3 => query.OrderBy(p => p.Inventories
                             .Where(i => i.BranchId == currentService.BranchId)
                             .Select(i => i.Quantity)
                             .FirstOrDefault()),
                null => query.OrderBy(p => p.Id),
                _ => throw new ArgumentException($"Invalid sort option: {sortId}")
            };

            // Apply includes last, after all IQueryable<Product> operations are done
            return await query
                .Include(p => p.Category)
                .Include(p => p.Inventories)
                .ToListAsync();
        }

    }
}