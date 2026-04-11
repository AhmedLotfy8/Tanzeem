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
        Task<SupplierResponseDto> GetSupplierByIdAsync(int id);
        
        Task<IEnumerable<SupplierResponseDto>> GetAllSuppliersAsync();

        Task<int> CreateSupplierAsync(SupplierRequestDto supplierDto);
       
        // Task<int> CsvUploadAsync(string filePath);

        Task<int> UpdateSupplierAsync(int id, SupplierRequestDto supplierDto);

        Task<bool> DeleteSupplierAsync(int id);

        Task<IEnumerable<SupplierLookupDto>> GetSuppliersLookupAsync();
    }
}
