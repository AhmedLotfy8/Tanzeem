using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Shared.Dtos.Products;
using Tanzeem.Shared.Dtos.Suppliers;

namespace Tanzeem.Services.Abstractions.Suppliers
{
    public interface ISupplierService
    {
        Task<SupplierDto> GetSupplierByIdAsync(int id);
        
        Task<IEnumerable<SupplierDto>> GetAllSuppliersAsync();

        Task<int> CreateSupplierAsync(SupplierDto supplierDto);
       
        // Task<int> CsvUploadAsync(string filePath);

        Task<int> UpdateSupplierAsync(int id, SupplierDto supplierDto);

        Task<bool> DeleteSupplierAsync(int id);
    }
}
