using Order.Data;
using Order.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service
{
    public class OrderService(IOrderRepository orderRepository) : IOrderService
    {
        public async Task<IEnumerable<OrderSummary>> GetOrdersAsync() =>
            await orderRepository.GetOrdersAsync();

        public async Task<IEnumerable<OrderSummary>> GetOrdersByStatusAsync(string orderStatus) => 
            await orderRepository.GetOrdersByStatusAsync(orderStatus);

        public async Task<OrderDetail> GetOrderByIdAsync(Guid orderId) =>
            await orderRepository.GetOrderByIdAsync(orderId);

        public async Task<OrderDetail> UpdateOrderStatusAsync(Guid orderId, string orderStatus) =>
            await orderRepository.UpdateOrderStatusAsync(orderId, orderStatus);

        public async Task<Guid?> CreateOrderAsync(CreateOrder order) =>
            await orderRepository.CreateOrderAsync(order);

        public async Task<IEnumerable<MonthProfit>> GetProfitByMonthAsync() =>
            await orderRepository.GetProfitByMonthAsync();
    }
}
