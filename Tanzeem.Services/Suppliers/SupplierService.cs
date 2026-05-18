using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Suppliers;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Tanzeem.Services.Suppliers
{
    public class SupplierService(IUnitOfWork _unitOfWork) : ISupplierService
    {
  
        public async Task<int> CreateSupplierAsync(SupplierRequestDto supplierDto)
        {
            #region mapping
            Supplier supplier = new Supplier
            {
                FullName = supplierDto.SupplierName,
                Email = supplierDto.Email,
                PhoneNumberOne = supplierDto.PhoneNumberOne,
                PhoneNumberTwo = supplierDto.PhoneNumberTwo,
                City = supplierDto.City,
                Country = supplierDto.Country,
                Street = supplierDto.Street,
                WebsiteURL = supplierDto.WebsiteURL,
                Notes = supplierDto.Notes,
                Tax_Id = supplierDto.Tax_Id,
                ContactPersonName = supplierDto.ContactPersonName,
                CompanyId = 4 ///TODO change CompanyId after auth
            };
            #endregion
            await _unitOfWork.GetRepository<Supplier>().AddAsync(supplier);
            await _unitOfWork.SaveChangesAsync();

            return supplier.Id;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
           var supplierToDelete = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(id);
            if (supplierToDelete != null)
            {
                _unitOfWork.GetRepository<Supplier>().DeleteAsync(supplierToDelete);
                await _unitOfWork.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<PaginationResponseDto<SupplierResponseDto>> GetAllSuppliersAsync
            (int page, int pageSize,SupplierFilter? supplierFilter = null,SupplierSort? supplierSort = null ,string? searchTerm = null)
        {
            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                .Where(supplier => supplier.CompanyId ==4);///TODO auth

            if (query == null)
            {
                throw new Exception("no data from supplier");///TODO exception handling
            }
            if (supplierFilter.HasValue)
            {
                switch (supplierFilter.Value)
                {
                    case SupplierFilter.ActiveSuppliers:
                        query = query.Where(s => s.SupplierStatus == SupplierStatus.Active);
                        break;
                    case SupplierFilter.InActiveSuppliers:
                        query = query.Where(s => s.SupplierStatus == SupplierStatus.InActive);
                        break;
                }
            }
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var cleanSearch = searchTerm.Trim().ToLower();

                bool isNumber = int.TryParse(cleanSearch, out int searchNumber);
                bool isDate = DateTime.TryParse(searchTerm, out DateTime searchDate);

                query = query.Where(supplier =>
                    supplier.FullName.ToLower().Contains(cleanSearch) ||
                    (supplier.Notes != null && supplier.Notes.ToLower().Contains(cleanSearch)) ||
                    (supplier.City != null && supplier.City.ToLower().Contains(cleanSearch)) ||
                    (supplier.ContactPersonName != null && supplier.ContactPersonName.ToLower().Contains(cleanSearch)) ||
                    (supplier.Country != null && supplier.Country.ToLower().Contains(cleanSearch)) ||
                    (supplier.Email != null && supplier.Email.ToLower().Contains(cleanSearch)) ||
                    (supplier.WebsiteURL != null && supplier.WebsiteURL.ToLower().Contains(cleanSearch)) ||
                    (supplier.PhoneNumberOne != null && supplier.PhoneNumberOne.ToLower().Contains(cleanSearch)) ||
                    (supplier.Tax_Id != null && supplier.Tax_Id.ToLower().Contains(cleanSearch)) ||
                    (isNumber && (supplier.Id == searchNumber))
                );
            }

            var totalCount = await query.CountAsync();

            if (supplierSort.HasValue)
            {
                switch (supplierSort.Value)
                {
                    case SupplierSort.AZSupplierName:
                        query = query.OrderBy(s => s.FullName);
                        break;
                    case SupplierSort.ZASupplierName:
                        query = query.OrderByDescending(s => s.FullName);
                        break;
                    case SupplierSort.AZCity:
                        query = query.OrderBy(s => s.City);
                        break;
                    case SupplierSort.ZACity:
                        query = query.OrderByDescending(s => s.City);
                        break;
                    case SupplierSort.HighOrdersCount:
                        query = query.OrderByDescending(s => s.Orders.Count());
                        break;
                    case SupplierSort.LowOrdersCount:
                        query = query.OrderBy(s => s.Orders.Count());
                        break;
                }

            }
            var suppliersFromDb = await query
            .Include(s => s.Orders)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
            #region mapping
            var supplierDtos = suppliersFromDb.Select(s => new SupplierResponseDto
            {
                SupplierName = s.FullName,
                Email = s.Email,
                PhoneNumberOne = s.PhoneNumberOne,
                PhoneNumberTwo = s.PhoneNumberTwo,
                City = s.City,
                Country = s.Country,
                Street = s.Street,
                WebsiteURL = s.WebsiteURL,
                Notes = s.Notes,
                Tax_Id = s.Tax_Id,
                ContactPersonName = s.ContactPersonName,

                onTimePercentage = SupplierServiceHelper.GetOnTimePercentage(s.Orders),

                LeadTime = SupplierServiceHelper.GetLeadTime(s.Orders),

                SupplierStatus = s.SupplierStatus,

                Badge = SupplierServiceHelper.GetBadge(s.Orders),

            });

            #endregion
            return new PaginationResponseDto<SupplierResponseDto>()
            {
                Data = supplierDtos,
                TotalCount = totalCount,
                CurrentPage= page,
                PageSize = pageSize
            };
        }

        public async Task<SupplierResponseDto> GetSupplierByIdAsync(int id)
        {
            var supplier = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(id);

            if (supplier == null) { return null!; }

            var supplierDto = new SupplierResponseDto
            {
                SupplierName = supplier.FullName,
                Email = supplier.Email,
                PhoneNumberOne = supplier.PhoneNumberOne,
                PhoneNumberTwo = supplier.PhoneNumberTwo,
                City = supplier.City,
                Country = supplier.Country,
                Street = supplier.Street,
                WebsiteURL = supplier.WebsiteURL,
                Notes = supplier.Notes,
                Tax_Id = supplier.Tax_Id,
                ContactPersonName = supplier.ContactPersonName,

                onTimePercentage = SupplierServiceHelper.GetOnTimePercentage(supplier.Orders),

                LeadTime = SupplierServiceHelper.GetLeadTime(supplier.Orders),

                SupplierStatus = supplier.SupplierStatus,

                Badge = SupplierServiceHelper.GetBadge(supplier.Orders),

            };
            return supplierDto;
        }

        public async Task<int> UpdateSupplierAsync(int id, SupplierRequestDto supplierDto)
        {
            var supplierToUpdate = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(id);

            if (supplierToUpdate == null)
            {
                throw new Exception("this supplier not found"); ///TODO exception handling
            }

            #region mapping

            supplierToUpdate.FullName = supplierDto.SupplierName;
            supplierToUpdate.Email = supplierDto.Email;
            supplierToUpdate.PhoneNumberOne = supplierDto.PhoneNumberOne;
            supplierToUpdate.PhoneNumberTwo = supplierDto.PhoneNumberTwo;
            supplierToUpdate.City = supplierDto.City;
            supplierToUpdate.Country = supplierDto.Country;
            supplierToUpdate.Street = supplierDto.Street;
            supplierToUpdate.WebsiteURL = supplierDto.WebsiteURL;
            supplierToUpdate.Notes = supplierDto.Notes;
            supplierToUpdate.Tax_Id = supplierDto.Tax_Id;
            supplierToUpdate.ContactPersonName = supplierDto.ContactPersonName;
            supplierToUpdate.CompanyId = 4; ///TODO change CompanyId after auth

            #endregion

            _unitOfWork.GetRepository<Supplier>().UpdateAsync(supplierToUpdate);
            await _unitOfWork.SaveChangesAsync();
            return supplierToUpdate.Id;
        }


        /// <summary>
        /// it used for field supplier name when you create new order
        /// </summary>
        /// <returns>supliers names</returns>
        public async Task<IEnumerable<SupplierLookupDto>> GetSuppliersLookupAsync()
        {
            var suppliers = await _unitOfWork.GetRepository<Supplier>().GetAllAsync();

            if (suppliers is null)
            {
                throw new Exception("no suppliers");
            }
            
            var supplierLookupDtos = suppliers.Select(s => new SupplierLookupDto
            {
                Id = s.Id,
                Name = s.FullName
            });
            return supplierLookupDtos;
        }

        public async Task<object> Counts()
        {
            var suppliers = _unitOfWork.GetRepository<Supplier>()
                .GetAllAsIQueryable()
                .Include(s => s.Orders)
                .Where(s => s.CompanyId == 4); ///TODO auth;

            int activeSuppliersCount = await suppliers.Where(s => s.SupplierStatus == SupplierStatus.Active).CountAsync();
            int in_activeSuppliersCount = await suppliers.Where(s => s.SupplierStatus == SupplierStatus.InActive).CountAsync();
           
            return new
            {
                ActiveSuppliersCount = activeSuppliersCount,
                InActiveSuppliersCount = in_activeSuppliersCount,
            };
        }

    }
}
