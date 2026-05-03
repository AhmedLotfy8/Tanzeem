using Microsoft.EntityFrameworkCore;
using Tanzeem.Domain.Contracts;
using Tanzeem.Domain.Entities.Branches;
using Tanzeem.Domain.Entities.Inventories;
using Tanzeem.Domain.Entities.Orders;
using Tanzeem.Domain.Entities.Products;
using Tanzeem.Domain.Entities.Suppliers;
using Tanzeem.Domain.Enums;
using Tanzeem.Services.Abstractions.Notifications;
using Tanzeem.Services.Abstractions.Orders;
using Tanzeem.Shared.Dtos;
using Tanzeem.Shared.Dtos.Orders;
using Tanzeem.Shared.Dtos.Products;

namespace Tanzeem.Services.Orders
{
    public class OrderService(IUnitOfWork _unitOfWork, INotificationService _notificationService) : IOrderService
    {
        public async Task<int> CreateOrderAsync(OrderRequestDto orderDto)
        {


            var supplier = await _unitOfWork.GetRepository<Supplier>().GetByIdAsync(orderDto.SupplierId);

            if (supplier == null)
                throw new Exception("This supplier not found");
            ///TODO change it after exception handling

            //check on orderTotal >0
            var productIds = orderDto.Items.Select(i => i.ProductId).Distinct().ToList();

            var existingProducts = await _unitOfWork.GetRepository<Product>()
                    .GetAllAsIQueryable()
                    .Where(product => productIds.Contains(product.Id))
                    .ToListAsync();

            if (existingProducts.Count != productIds.Count)
                throw new Exception("One or more products were not found!");
            ///TODO after exceptionHandling
        
            #region mapping
            var OrderItems = orderDto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Price * item.Quantity,
            }).ToList();
            
            Order order = new Order //status must be default as pending
            {
                OrderDate = orderDto.OrderDate,
                Total = OrderServiceHelper.calculateTotalOfOrder(OrderItems, orderDto.Taxes, orderDto.ShippingCost),
                SupplierId = orderDto.SupplierId,
                Taxes = orderDto.Taxes,
                ShippingCost = orderDto.ShippingCost,
                ExpectedDeliveryDate = orderDto.ExpectedDeliveryDate,
                Notes = orderDto.Notes,
                SupplierName = supplier.FullName,
                Items = OrderItems,
                Status = orderDto.Status,
                ///TODO change branch after auth
                BranchId = 2,
            };
            #endregion

            await _unitOfWork.GetRepository<Order>().AddAsync(order);

            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0) throw new Exception("Failed to create order");
            ///TODO after exceptionHandling

            return order.Id;
        }

        public async Task<bool> DeleteOrderAsync(int id)
        {
            var orderToDelete = await _unitOfWork.GetRepository<Order>().GetByIdAsync(id);

            if (orderToDelete is null)
                throw new Exception("No order to delete");

            _unitOfWork.GetRepository<Order>().DeleteAsync(orderToDelete);

            int affectedRows = await _unitOfWork.SaveChangesAsync();

            if (affectedRows <= 0)
                throw new Exception("No rows affected");
            ///TODO exception handling

            return true;
        }

        //public async Task<IEnumerable<OrderSummaryResponseDto>> GetAllOrdersAsync()
        //{
        //    var query = _unitOfWork.GetRepository<Order>().GetAllAsIQueryable();

        //    if (!query.Any())
        //        throw new Exception("No orders");
        //    ///TODO exception handling

        //    var orderDtos = await query
        //    .Select(order => new OrderSummaryResponseDto
        //    {
        //        Id = order.Id,
        //        OrderDate = order.OrderDate,
        //        SupplierName = order.SupplierName,
        //        Total = order.Total,
        //        Status = order.Status.ToString(),
        //    }
        //    ).ToListAsync();

        //    return orderDtos;
        //}

        public async Task<OrderResponseDto> GetOrderByIdAsync(int id)
        {
            var query = _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(id);

            var orderQuery = query.Include(o => o.Items).ThenInclude(p => p.Product);

            var order = await orderQuery.FirstOrDefaultAsync();

            if (order is null)
            {
                throw new Exception($"This order {id} not found");
                ///TODO exception handling
            }

            var orderItems = order.Items.Select(item => new OrderItemResponseDto
            {
                ProductName = item.Product?.Name ?? "N/A",
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Total,
            }).ToList();

            #region mapping
            OrderResponseDto orderDto = new OrderResponseDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                ExpectedDeliveryDate = order.ExpectedDeliveryDate,
                RecievedDeliveryDate = order.RecievedDeliveryDate ?? null,
                SupplierName = order.SupplierName,
                Total = order.Total,
                Taxes = order.Taxes,
                ShippingCost = order.ShippingCost,
                Status = order.Status.ToString(),
                Items = orderItems,
                SubTotal = orderItems.Sum(i => i.Total),
                Notes = order.Notes,
            };
            #endregion
            return orderDto;
        }

        public async Task<int> UpdateOrderAsync(int id, OrderRequestDto orderDto)
        {
            var orderToUpdate = _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(id);
            var order = await orderToUpdate.Include(i => i.Items).FirstOrDefaultAsync();

            if (order == null)
            {
                throw new Exception($"This order {id} not found");
                ///TODO exception handling
            }
            #region mapping
            order.OrderDate = orderDto.OrderDate;
            order.ExpectedDeliveryDate = orderDto.ExpectedDeliveryDate;
            order.RecievedDeliveryDate = orderDto.RecievedDeliveryDate;
            order.ShippingCost = orderDto.ShippingCost;
            order.Taxes = orderDto.Taxes;
            order.Status = orderDto.Status;
            order.Notes = orderDto.Notes;


            #region items mapping
            order.Items.Clear();

            var newItems = orderDto.Items.Select(item => new OrderItem
            {
                ProductId = item.ProductId,
                Price = item.Price,
                Quantity = item.Quantity,
                Total = item.Price * item.Quantity,
            }).ToList();

            foreach (var item in newItems)
            {
                order.Items.Add(item);
            }
            #endregion
            order.Total = OrderServiceHelper.calculateTotalOfOrder(newItems, orderDto.Taxes, orderDto.ShippingCost);
            #endregion

            _unitOfWork.GetRepository<Order>().UpdateAsync(order);

            int rowsAffected = await _unitOfWork.SaveChangesAsync();

            if (rowsAffected <= 0)
                throw new Exception("No update happened");
            ///TODO exception handling

            return order.Id;
        }

        public async Task<PaginationResponseDto<OrderSummaryResponseDto>> GetOrdersWithPaginationAsync(int page, int pageSize)
        {
            if (page <= 0) page = 1;

            const int maxPageSize = 10;
            
            if (pageSize > maxPageSize) pageSize = maxPageSize;

            var query = _unitOfWork.GetRepository<Order>().GetAllAsIQueryable();

            var totalCount = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedData = orders.Select(order => new OrderSummaryResponseDto
            {
                Id = order.Id,
                OrderDate = order.OrderDate,
                SupplierName = order.SupplierName,
                Total = order.Total,
                Status = order.Status.ToString(),
            }).ToList();

            return new PaginationResponseDto<OrderSummaryResponseDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = mappedData
            };
        }

        public async Task<IEnumerable<ProductLookupDto>> GetProductsLookupAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return Enumerable.Empty<ProductLookupDto>();

            var query = _unitOfWork.GetRepository<Product>().GetAllAsIQueryable();

            var selectedProducts = await query.Where(p => p.Name.Contains(searchTerm) || p.SKU.StartsWith(searchTerm))
            .Select(p => new ProductLookupDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.CostPrice,
            }).Take(15).ToListAsync();

            return selectedProducts;
        }

        public async Task<string> ChangeOrderToDeliverd(OrderConfirmDto confirmDto, int id)
        {
            var order = _unitOfWork.GetRepository<Order>().GetByIdAsQueryable(id)
                .Include(o => o.Items).FirstOrDefault();

            if (order == null)
                throw new Exception("No order with this id");

            if (order.Status == OrderStatus.Deliverd)
            {
                return "order already deliverd";
            }

            ///TODO exception handling
            if (order.Items == null || !order.Items.Any())
            {
                throw new Exception("you cannot receive empty order");
            }

            if (confirmDto.ItemsConfirmDtos.Count() != order.Items.Count)
            {
                throw new Exception("there are some items deleted");
            }

            var orderIds = order.Items.Select(i => i.ProductId);

            var products = _unitOfWork.GetRepository<Product>().GetAllAsIQueryable().AsTracking()
                .Where(product => orderIds.Contains(product.Id)).ToList();

            if (!products.Any())
            {
                throw new Exception("No products");
            }
            ///TODO exception handling

            var inventories = _unitOfWork.GetRepository<Inventory>().GetAllAsIQueryable().AsTracking()
                .Where(inv => orderIds.Contains(inv.ProductId)).ToList();

            if (!inventories.Any())
            {
                throw new Exception("No inventories");
            }
            ///TODO exception handling
            if (confirmDto is null)
                throw new Exception("empty confirmation fields");

            #region changes after deliver
            order.Status = Domain.Enums.OrderStatus.Deliverd;
            order.RecievedDeliveryDate = confirmDto.RecievedDate ?? DateTime.Now;

            foreach (var product in products)
            {
                var itemsConfirm = confirmDto!.ItemsConfirmDtos.FirstOrDefault(confirmDto => confirmDto.ProductId == product.Id);

                if (itemsConfirm != null)
                {
                    product.CostPrice = itemsConfirm.CostPrice;
                    product.SellingPrice = itemsConfirm.SellPrice;
                }
            }

            foreach (var inventory in inventories)
            {
                var itemsConfirm = confirmDto!.ItemsConfirmDtos.FirstOrDefault(confirmDto => confirmDto.ProductId == inventory.ProductId && inventory.BranchId == 1);
                ///TODO change after authorization

                var originalOrderItem = order.Items.FirstOrDefault(i => i.ProductId == inventory.ProductId);

                if (itemsConfirm != null && originalOrderItem != null)
                {
                    inventory.Quantity = inventory.Quantity + originalOrderItem.Quantity - itemsConfirm.DamagedQuantity;
                }
            }
            #endregion

            int rowsAffected = await _unitOfWork.SaveChangesAsync();

            await _notificationService.CreateOrderDeliveredNotification(order.Id);
            
            if (rowsAffected <= 0)
                throw new Exception("Status not changed");
            ///TODO exception handling
            
            return order.Status.ToString();
        }

        //public IEnumerable<object> DisplayOrderStatuses()
        //{
        //    return Enum.GetValues<OrderStatus>()
        //   .Select(s => new { Id = (int)s, Name = s.ToString() });
        //}

        //public int CountPendingOrders()
        //{
        //    return _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
        //        .Count(o => o.Status == OrderStatus.Pending);
        //}

        //public int CountDeliverdOrders()
        //{
        //    return _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
        //        .Count(o => o.Status == OrderStatus.Deliverd);
        //}
    
        public async Task<object> Counts()
        {
            var pendingCount = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                .CountAsync(o => o.Status == OrderStatus.Pending);

            var deliveredCount = await _unitOfWork.GetRepository<Order>().GetAllAsIQueryable()
                .CountAsync(o => o.Status == OrderStatus.Deliverd);

            var TotalRevenue = await _unitOfWork.GetRepository<OrderItem>().GetAllAsIQueryable()
                .Include(o => o.Order)
                .Where(oi => oi.Order.Status == OrderStatus.Deliverd)
                .SumAsync(oi => oi.Quantity * oi.Price);
            
            var roundedRevenue = Math.Round(TotalRevenue);
            
            return new
            {
                pendingOrdersCount = pendingCount,
                deliveredOrdersCount = deliveredCount,
                TotalOrdersRevenue = roundedRevenue
            };
        }

    }
}
