using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.CustomExceptions;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Domain.Exceptions;
using Tanzeem.Services.Abstractions.Current;
using Tanzeem.Services.Abstractions.Suppliers;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Suppliers;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using ValidationException = Tanzeem.Domain.Exceptions.ValidationException;

namespace Tanzeem.Services.Suppliers
{
    public class SupplierService(IUnitOfWork _unitOfWork,ICurrentService _currentService): ISupplierService
    {
        
        public async Task<int> CreateSupplierAsync(SupplierRequestDto supplierDto)
        {
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            #region validation dto 
            if (supplierDto is null)
                throw new ValidationException("Empty fields");
            
            if (string.IsNullOrWhiteSpace(supplierDto.SupplierName))
                throw new ValidationException("Supplier name is required.");

            var isDuplicate = await _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                    .AnyAsync(s => s.CompanyId == companyId &&
                      (s.Email == supplierDto.Email.Trim() ||
                      (!string.IsNullOrEmpty(supplierDto.Tax_Id) && s.Tax_Id == supplierDto.Tax_Id.Trim())));

            if (isDuplicate)
                throw new BusinessRuleException("A supplier with the same email or tax ID already exists in this company.");
            
            string cleanPhone = supplierDto.PhoneNumberOne.Replace(" ", "").Replace("-", "");
            string cleanPhone2 = supplierDto?.PhoneNumberTwo?.Replace(" ", "").Replace("-", "") ?? "-";

            if (cleanPhone.Length > 20 || cleanPhone2.Length > 20)
            {
                throw new ValidationException("Phone number is too long. Maximum allowed is 20 digits.");
            }
            #endregion


            var lastSupplier = await _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.Id)
                .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastSupplier != null && !string.IsNullOrWhiteSpace(lastSupplier.SupplierNumber))
            {
                string[] numberParts = lastSupplier.SupplierNumber.Split('-');
                if (numberParts.Length > 0 && int.TryParse(numberParts.Last(), out int lastSeq))
                {
                    nextNumber = lastSeq + 1;
                }
            }

            string generatedSupplierNumber = $"SUP-{nextNumber:D4}";
            #region mapping
            Supplier supplier = new Supplier
            {
                FullName = supplierDto!.SupplierName.Trim(), 
                Email = supplierDto.Email.Trim(),
                PhoneNumberOne = cleanPhone,
                PhoneNumberTwo = cleanPhone2,
                City = supplierDto.City,
                Country = supplierDto.Country,
                Street = supplierDto.Street,
                WebsiteURL = supplierDto.WebsiteURL?.Trim(),
                Notes = supplierDto.Notes,
                Tax_Id = supplierDto.Tax_Id?.Trim(),
                ContactPersonName = supplierDto.ContactPersonName?.Trim(),
                SupplierStatus = supplierDto.SupplierStatus,
                CompanyId = companyId,
                SupplierNumber = generatedSupplierNumber
            };
            #endregion
            await _unitOfWork.GetRepository<Supplier>().AddAsync(supplier);
            int rowsAffected = await _unitOfWork.SaveChangesAsync();

            if (rowsAffected <= 0)
                throw new DbUpdateFailedException("Failed to save the new supplier. Please try again.");

            return supplier.Id;
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            var supplierToDelete = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(id);

            if (supplierToDelete == null || supplierToDelete.CompanyId != companyId)
            {
                throw new KeyNotFoundException("this supplier not found");
            }

            _unitOfWork.GetRepository<Supplier>().DeleteAsync(supplierToDelete);               

            int affectedRows = await _unitOfWork.SaveChangesAsync();
            
            if (affectedRows <= 0)
            {
                throw new DbUpdateFailedException("No delete happened, please try again later.");
            }
            return true;
        }

        public async Task<PaginationResponseDto<SupplierResponseDto>> GetAllSuppliersAsync
            (int page, int pageSize,SupplierFilter? supplierFilter = null,SupplierSort? supplierSort = null ,string? searchTerm = null)
        {
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            if (page <= 0) page = 1;

            const int maxPageSize = 20;

            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                .Where(supplier => supplier.CompanyId ==companyId);

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
                    supplier.SupplierNumber.Contains(cleanSearch) ||
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
            else
            {
                query = query.OrderByDescending(s => s.Id);
            }

            var suppliersFromDb = await query
            .Include(s => s.Orders)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
            
            #region mapping
            var supplierDtos = suppliersFromDb.Select(s => new SupplierResponseDto
            {
                Id = s.Id,
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
                SupplierNumber = s.SupplierNumber,

            }).ToList();

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
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            var supplier = await _unitOfWork.GetRepository<Supplier>().GetByIdAsQueryable(id)
                .Include(s => s.Orders)
                .FirstOrDefaultAsync();

            if (supplier == null || supplier.CompanyId != companyId)
            {
                throw new KeyNotFoundException("No supplier with this id");
            }

            var supplierDto = new SupplierResponseDto
            {
                Id = supplier.Id,
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
                SupplierNumber = supplier.SupplierNumber,

            };
            return supplierDto;
        }

        public async Task<int> UpdateSupplierAsync(int id, SupplierRequestDto supplierDto)
        {
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            if (supplierDto is null)
                throw new ValidationException("Empty fields");

            if (string.IsNullOrWhiteSpace(supplierDto.SupplierName))
                throw new ValidationException("Supplier name is required.");
            
            var emailToCheck = supplierDto.Email?.Trim();
            var taxIdToCheck = supplierDto.Tax_Id?.Trim();
            string cleanPhone = supplierDto.PhoneNumberOne.Replace(" ", "").Replace("-", "");
            string cleanPhone2 = supplierDto?.PhoneNumberTwo?.Replace(" ", "").Replace("-", "") ?? "-";
            if (cleanPhone.Length > 20 || cleanPhone2.Length > 20)
            {
                throw new ValidationException("Phone number is too long. Maximum allowed is 20 digits.");
            }
            var isDuplicate = await _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                        .AnyAsync(s => s.CompanyId == companyId &&
                       s.Id != id &&
                       ((!string.IsNullOrEmpty(emailToCheck) && s.Email == emailToCheck) ||
                        (!string.IsNullOrEmpty(taxIdToCheck) && s.Tax_Id == taxIdToCheck)));

            if (isDuplicate)
                throw new BusinessRuleException("A supplier with the same email or tax ID already exists in this company.");


            var supplierToUpdate = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(id);

            if (supplierToUpdate == null || supplierToUpdate.CompanyId != companyId)
            {
                throw new KeyNotFoundException("this supplier not found");
            }

            #region mapping

            supplierToUpdate.FullName = supplierDto!.SupplierName.Trim();
            supplierToUpdate.Email = emailToCheck!;
            supplierToUpdate.PhoneNumberOne = cleanPhone;
            supplierToUpdate.PhoneNumberTwo = cleanPhone2;
            supplierToUpdate.City = supplierDto.City;
            supplierToUpdate.Country = supplierDto.Country;
            supplierToUpdate.Street = supplierDto.Street;
            supplierToUpdate.WebsiteURL = supplierDto.WebsiteURL?.Trim();
            supplierToUpdate.Notes = supplierDto.Notes;
            supplierToUpdate.Tax_Id = taxIdToCheck;
            supplierToUpdate.ContactPersonName = supplierDto.ContactPersonName?.Trim();
            supplierToUpdate.SupplierStatus = supplierDto.SupplierStatus;

            #endregion

            _unitOfWork.GetRepository<Supplier>().UpdateAsync(supplierToUpdate);
            await _unitOfWork.SaveChangesAsync();
            return supplierToUpdate.Id;
        }


        /// <summary>
        /// it used for field supplier name when you create new order
        /// </summary>
        /// <returns>supliers names</returns>
        
        public async Task<IEnumerable<SupplierLookupDto>> GetSuppliersLookupAsync(string? searchTerm = null)
        {
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            var query = _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
                .Where(x => x.CompanyId == companyId && x.SupplierStatus == SupplierStatus.Active);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var cleanSearch = searchTerm.Trim();
                query = query.Where(x => x.FullName.Contains(cleanSearch));
            }

            var suppliers = await query.OrderBy(x => x.FullName)
                .Select(s => new SupplierLookupDto
                {
                    Id = s.Id,
                    Name = s.FullName,
                    Number = s.SupplierNumber,
                })
                .Take(50)
                .ToListAsync();

            return suppliers;
        }

        public async Task<SupplierCountsDto> Counts()
        {
            //int companyId = 4;
            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company id assigned"); 

            var baseQuery = _unitOfWork.GetRepository<Supplier>()
                .GetAllAsIQueryable()
                .Where(s => s.CompanyId == companyId);

            int activeCount = await baseQuery.CountAsync(s => s.SupplierStatus == SupplierStatus.Active);
            int inactiveCount = await baseQuery.CountAsync(s => s.SupplierStatus == SupplierStatus.InActive);


            return new SupplierCountsDto
            {
                ActiveSuppliersCount = activeCount,
                InActiveSuppliersCount = inactiveCount
            };
        }

        public async Task<int> ImportSuppliersFromCsvAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ValidationException("Please upload a valid CSV file.");

            int companyId = _currentService.CompanyId ?? throw new UnauthorizedAccessException("No company assigned.");

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                Delimiter = ",",
                MissingFieldFound = null,
                HeaderValidated = null
            };

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var lastSupplier = await _unitOfWork.GetRepository<Supplier>().GetAllAsIQueryable()
            .Where(s => s.CompanyId == companyId)
            .OrderByDescending(s => s.Id)
            .FirstOrDefaultAsync();

            int nextNumber = 1;

            if (lastSupplier != null && !string.IsNullOrWhiteSpace(lastSupplier.SupplierNumber))
            {
                string[] numberParts = lastSupplier.SupplierNumber.Split('-');
                if (numberParts.Length > 0 && int.TryParse(numberParts.Last(), out int lastSeq))
                {
                    nextNumber = lastSeq + 1;
                }
            }

            var suppliersToInsert = new List<Supplier>();

            while (csv.Read())
            {
                string name = csv.GetField<string>("Name") ?? "N/A";
                string email = csv.GetField<string>("Email") ?? "N/A";
                string phone1 = csv.GetField<string>("Phone 1") ?? "N/A";
                string phone2 = csv.GetField<string>("Phone 2") ?? "N/A";
                string Street = csv.GetField<string>("Street") ?? "N/A";
                string City = csv.GetField<string>("City") ?? "N/A";
                string Country = csv.GetField<string>("Country") ?? "N/A";

                string cleanPhone1 = phone1?.Replace(" ", "").Replace("-", "") ?? "";
                string cleanPhone2 = phone2?.Replace(" ", "").Replace("-", "") ?? "";

                if (cleanPhone1.Length > 20) cleanPhone1 = cleanPhone1.Substring(0, 20);
                if (cleanPhone2.Length > 20) cleanPhone2 = cleanPhone2.Substring(0, 20);

                var supplier = new Supplier
                {
                    SupplierNumber = $"SUP-{nextNumber:D4}",
                    FullName = name,
                    Email = string.IsNullOrWhiteSpace(email) ? "N/A" : email,
                    PhoneNumberOne = cleanPhone1,
                    PhoneNumberTwo = string.IsNullOrWhiteSpace(cleanPhone2) ? null : cleanPhone2,
                    
                    Street = string.IsNullOrWhiteSpace(Street) ? "N/A" : Street,
                    City = string.IsNullOrWhiteSpace(City) ? "N/A" : City,
                    Country = string.IsNullOrWhiteSpace(Country) ? "N/A" : Country,
                    CompanyId = companyId
                };
                nextNumber++;
                suppliersToInsert.Add(supplier);
            }

            if (suppliersToInsert.Any())
            {
                await _unitOfWork.GetRepository<Supplier>().AddRangeAsync(suppliersToInsert);
                await _unitOfWork.SaveChangesAsync();
            }

            return suppliersToInsert.Count;
        }
    }
}
