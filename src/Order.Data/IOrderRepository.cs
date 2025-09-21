using Order.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Order.Data
{
    public interface IOrderRepository
    {
        Task<IEnumerable<OrderSummary>> GetOrdersAsync();
        
        Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string orderStatus);

        Task<OrderDetail> GetOrderByIdAsync(Guid orderId);
        
        Task<OrderDetail> UpdateOrderStatusAsync(Guid orderId, string orderStatus);

        Task<Guid?> CreateOrderAsync(CreateOrder order);
        
        Task<IEnumerable<MonthProfit>> GetProfitByMonthAsync();
    }
}
