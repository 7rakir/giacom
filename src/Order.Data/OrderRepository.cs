using Microsoft.EntityFrameworkCore;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Order.Data
{
    public class OrderRepository(OrderContext orderContext) : IOrderRepository
    {
        private const string CreatedOrderStatus = "Created";
        private const string CompletedOrderStatus = "Completed";

        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync()
        {
            return await orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .OrderByDescending(x => x.CreatedDate)
                .Select(MapToSummaryExpression)
                .ToListAsync();
        }

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string orderStatus)
        {
            return await orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .OrderByDescending(x => x.CreatedDate)
                .Where(x => x.Status.Name == orderStatus)
                .Select(MapToSummaryExpression)
                .ToListAsync();
        }

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId)
        {
            return await GetOrderByIdQuery(orderId)
                .Select(MapToDetailExpression)
                .SingleOrDefaultAsync();
        }

        public async Task<OrderDetail> UpdateOrderStatusAsync(Guid orderId, string orderStatus)
        {
            var statusId = await GetStatusId(orderStatus);
            if (statusId == null)
            {
                return null;
            }

            var order = await GetOrderByIdQuery(orderId).SingleOrDefaultAsync();
            if (order == null)
            {
                return null;
            }
            order.StatusId = statusId;
            await orderContext.SaveChangesAsync();
            return MapToDetailExpression.Compile().Invoke(order);
        }

        private async Task<byte[]> GetStatusId(string orderStatus)
        {
            return await orderContext.OrderStatus
                .Where(x => x.Name == orderStatus)
                .Select(x => x.Id)
                .SingleOrDefaultAsync();
        }

        public async Task<Guid?> CreateOrderAsync(CreateOrder order)
        {
            var statusId = await GetStatusId(CreatedOrderStatus);
            if (statusId == null)
            {
                return null;
            }

            var orderId = Guid.NewGuid();
            var newOrder = new Entities.Order
            {
                Id = orderId.ToByteArray(),
                ResellerId = order.ResellerId.ToByteArray(),
                CustomerId = order.CustomerId.ToByteArray(),
                StatusId = statusId,
                CreatedDate = DateTime.UtcNow,
                Items = order.Items.Select(item => new Entities.OrderItem
                {
                    Id = Guid.NewGuid().ToByteArray(),
                    ServiceId = item.ServiceId.ToByteArray(),
                    ProductId = item.ProductId.ToByteArray(),
                    Quantity = item.Quantity
                }).ToArray()
            };

            orderContext.Order.Add(newOrder);
            await orderContext.SaveChangesAsync();
            return orderId;
        }

        public async Task<IEnumerable<MonthProfit>> GetProfitByMonthAsync()
        {
            var profitByDate = await orderContext.Order
                .Include(x => x.Items)
                .Include(x => x.Status)
                .Where(x => x.Status.Name == CompletedOrderStatus)
                .Select(x => new
                {
                    x.CreatedDate,
                    OrderProfit = x.Items.Sum(i => i.Quantity * (i.Product.UnitPrice - i.Product.UnitCost)) ?? 0
                })
                .ToArrayAsync();
                
                
            return profitByDate.GroupBy(x => x.CreatedDate.Month)
                .Select(x => new MonthProfit
                {
                    Month = x.Key,
                    Profit = x.Sum(y => y.OrderProfit)
                })
                .OrderBy(x => x.Month)
                .ToArray();
        }
        
        private IQueryable<Entities.Order> GetOrderByIdQuery(Guid orderId)
        {
            var orderIdBytes = orderId.ToByteArray();

            return orderContext.Order
                .Where(x => orderContext.Database.IsInMemory()
                    ? x.Id.SequenceEqual(orderIdBytes)
                    : x.Id == orderIdBytes);
        }
        
        private static Expression<Func<Entities.Order, OrderSummary>> MapToSummaryExpression =>
            order => new OrderSummary
            {
                Id = new Guid(order.Id),
                ResellerId = new Guid(order.ResellerId),
                CustomerId = new Guid(order.CustomerId),
                StatusId = new Guid(order.StatusId),
                StatusName = order.Status.Name,
                ItemCount = order.Items.Count,
                TotalCost = order.Items.Sum(i => i.Quantity * i.Product.UnitCost) ?? 0,
                TotalPrice = order.Items.Sum(i => i.Quantity * i.Product.UnitPrice) ?? 0,
                CreatedDate = order.CreatedDate
            };
        
        private static Expression<Func<Entities.Order, OrderDetail>> MapToDetailExpression =>
            order => new OrderDetail
            {
                Id = new Guid(order.Id),
                ResellerId = new Guid(order.ResellerId),
                CustomerId = new Guid(order.CustomerId),
                StatusId = new Guid(order.StatusId),
                StatusName = order.Status.Name,
                CreatedDate = order.CreatedDate,
                TotalCost = order.Items.Sum(i => i.Quantity * i.Product.UnitCost) ?? 0,
                TotalPrice = order.Items.Sum(i => i.Quantity * i.Product.UnitPrice) ?? 0,
                Items = order.Items.Select(i => new OrderItem
                {
                    Id = new Guid(i.Id),
                    OrderId = new Guid(i.OrderId),
                    ServiceId = new Guid(i.ServiceId),
                    ServiceName = i.Service.Name,
                    ProductId = new Guid(i.ProductId),
                    ProductName = i.Product.Name,
                    UnitCost = i.Product.UnitCost,
                    UnitPrice = i.Product.UnitPrice,
                    TotalCost = i.Product.UnitCost * i.Quantity ?? 0,
                    TotalPrice = i.Product.UnitPrice * i.Quantity ?? 0,
                    Quantity = i.Quantity ?? 0
                })
            };
    }
}
