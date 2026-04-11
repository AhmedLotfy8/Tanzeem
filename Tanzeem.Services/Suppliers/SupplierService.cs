using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Shared.Dtos.Suppliers;

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

        public async Task<IEnumerable<SupplierResponseDto>> GetAllSuppliersAsync()
        {
            var suppliers = await _unitOfWork.GetRepository<Supplier>().GetAllAsync(o => o.Orders);

            #region mapping
            var supplierDtos = suppliers.Select(s => new SupplierResponseDto
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

                Status = SupplierServiceHelper.GetSupplierStatus(s.Orders).ToString(),

                 Badge = SupplierServiceHelper.GetBadge(s.Orders),

            }).ToList();

            #endregion
            return supplierDtos;
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

                Status = SupplierServiceHelper.GetSupplierStatus(supplier.Orders).ToString(),

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
    }
}
