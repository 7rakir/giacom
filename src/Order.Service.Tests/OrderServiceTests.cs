using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using NUnit.Framework;
using Order.Data;
using Order.Data.Entities;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace Order.Service.Tests
{
    public class OrderServiceTests
    {
        private IOrderService _orderService;
        private IOrderRepository _orderRepository;
        private OrderContext _orderContext;
        private DbConnection _connection;

        private readonly Guid _orderStatusCreatedId = Guid.NewGuid();
        private readonly Guid _orderStatusCompletedId = Guid.NewGuid();
        private readonly Guid _orderServiceEmailId = Guid.NewGuid();
        private readonly Guid _orderProductEmailId = Guid.NewGuid();
        private readonly Guid _orderProductEmailEnhancedId = Guid.NewGuid();


        [SetUp]
        public async Task Setup()
        {
            var options = new DbContextOptionsBuilder<OrderContext>()
                .UseSqlite(CreateInMemoryDatabase())
                .EnableDetailedErrors(true)
                .EnableSensitiveDataLogging(true)
                .Options;

            _connection = RelationalOptionsExtension.Extract(options).Connection;

            _orderContext = new OrderContext(options);
            _orderContext.Database.EnsureDeleted();
            _orderContext.Database.EnsureCreated();

            _orderRepository = new OrderRepository(_orderContext);
            _orderService = new OrderService(_orderRepository);

            await AddReferenceDataAsync(_orderContext);
        }

        [TearDown]
        public void TearDown()
        {
            _connection.Dispose();
            _orderContext.Dispose();
        }


        private static DbConnection CreateInMemoryDatabase()
        {
            var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            return connection;
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsCorrectNumberOfOrders()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, _orderStatusCreatedId, DateTime.Now, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, _orderStatusCreatedId, DateTime.Now, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, _orderStatusCreatedId, DateTime.Now, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            Assert.AreEqual(3, orders.Count());
        }

        [Test]
        public async Task GetOrdersAsync_ReturnsOrdersWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, _orderStatusCreatedId, DateTime.Now, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, _orderStatusCreatedId, DateTime.Now, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, _orderStatusCreatedId, DateTime.Now, 3);

            // Act
            var orders = await _orderService.GetOrdersAsync();

            // Assert
            var order1 = orders.SingleOrDefault(x => x.Id == orderId1);
            var order2 = orders.SingleOrDefault(x => x.Id == orderId2);
            var order3 = orders.SingleOrDefault(x => x.Id == orderId3);

            Assert.AreEqual(0.8m, order1.TotalCost);
            Assert.AreEqual(0.9m, order1.TotalPrice);

            Assert.AreEqual(1.6m, order2.TotalCost);
            Assert.AreEqual(1.8m, order2.TotalPrice);

            Assert.AreEqual(2.4m, order3.TotalCost);
            Assert.AreEqual(2.7m, order3.TotalPrice);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrder()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, _orderStatusCreatedId, DateTime.Now, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(orderId1, order.Id);
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsCorrectOrderItemCount()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, _orderStatusCreatedId, DateTime.Now, 1);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1, order.Items.Count());
        }

        [Test]
        public async Task GetOrderByIdAsync_ReturnsOrderWithCorrectTotals()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, _orderStatusCreatedId, DateTime.Now, 2);

            // Act
            var order = await _orderService.GetOrderByIdAsync(orderId1);

            // Assert
            Assert.AreEqual(1.6m, order.TotalCost);
            Assert.AreEqual(1.8m, order.TotalPrice);
        }
        
        [Test]
        public async Task GetOrdersByStatusAsync_NoOrders_ReturnsEmptyOrders()
        {
            // Act
            var orders = await _orderService.GetOrdersByStatusAsync("Created");

            // Assert
            Assert.IsEmpty(orders);
        }
        
        [Test]
        public async Task GetOrdersByStatusAsync_NonExistingStatus_ReturnsEmptyOrders()
        {
            // Arrange
            var orderId1 = Guid.NewGuid();
            await AddOrder(orderId1, _orderStatusCreatedId, DateTime.Now, 1);

            var orderId2 = Guid.NewGuid();
            await AddOrder(orderId2, _orderStatusCreatedId, DateTime.Now, 2);

            var orderId3 = Guid.NewGuid();
            await AddOrder(orderId3, _orderStatusCreatedId, DateTime.Now, 3);

            // Act
            var orders = await _orderService.GetOrdersByStatusAsync("Invalid Status");

            // Assert
            Assert.IsEmpty(orders);
        }
        
        [Test]
        public async Task GetOrdersByStatusAsync_ReturnsOrdersWithSpecifiedStatus()
        {
            // Arrange
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Now, 1);
            await AddOrder(Guid.NewGuid(), _orderStatusCreatedId, DateTime.Now, 2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Now, 3);

            // Act
            var orders = await _orderService.GetOrdersByStatusAsync("Completed");

            // Assert
            Assert.AreEqual(2, orders.Count());
            Assert.IsTrue(orders.All(o => o.StatusName == "Completed"));
            
        }
        
        [Test]
        public async Task UpdateOrderStatusAsync_NonExistingOrder_ReturnsNull()
        {
            // Arrange
            var nonExistingOrderId = Guid.NewGuid();

            // Act
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(nonExistingOrderId, "Completed");
            
            // Assert
            Assert.IsNull(updatedOrder);
        }
        
        [Test]
        public async Task UpdateOrderStatusAsync_ToSameStatus_StatusIsSame()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrder(orderId, _orderStatusCreatedId, DateTime.Now, 1);

            // Act
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(orderId, "Created");

            // Assert
            Assert.AreEqual(orderId, updatedOrder.Id);
            Assert.AreEqual("Created", updatedOrder.StatusName);
        }

        [Test]
        public async Task UpdateOrderStatusAsync_ToDifferentStatus_UpdatesStatus()
        {
            // Arrange
            var orderId = Guid.NewGuid();
            await AddOrder(orderId, _orderStatusCreatedId, DateTime.Now, 1);

            // Act
            var updatedOrder = await _orderService.UpdateOrderStatusAsync(orderId, "Completed");

            // Assert
            Assert.AreEqual(orderId, updatedOrder.Id);
            Assert.AreEqual("Completed", updatedOrder.StatusName);
        }

        [Test]
        public async Task CreateOrderAsync_WithMultipleOrderItems_CreatesOrder()
        {
            // Arrange
            var createOrder = new Model.CreateOrder
            {
                ResellerId = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                Items =
                [
                    new Model.CreateOrderItem
                    {
                        ServiceId = _orderServiceEmailId,
                        ProductId = _orderProductEmailId,
                        Quantity = 2
                    },
                    new Model.CreateOrderItem
                    {
                        ServiceId = _orderServiceEmailId,
                        ProductId = _orderProductEmailEnhancedId,
                        Quantity = 1
                    }
                ]
            };

            // Act
            var newOrderId = await _orderService.CreateOrderAsync(createOrder);

            // Assert
            Assert.IsNotNull(newOrderId);
            
            var newOrder = await _orderService.GetOrderByIdAsync(newOrderId.Value);
            
            Assert.IsNotNull(newOrder);
            Assert.AreEqual(createOrder.ResellerId, newOrder.ResellerId);
            Assert.AreEqual(createOrder.CustomerId, newOrder.CustomerId);
            Assert.AreEqual("Created", newOrder.StatusName);
            Assert.AreEqual(2, newOrder.Items.Count());

            var firstItem = newOrder.Items.Single(i => i.ProductId == _orderProductEmailId);
            Assert.AreEqual(2, firstItem.Quantity);
            
            var secondItem = newOrder.Items.Single(i => i.ProductId == _orderProductEmailEnhancedId);
            Assert.AreEqual(1, secondItem.Quantity);
        }

        [Test]
        public async Task CalculateProfitByMonthAsync_NoOrders_ReturnsEmptyProfit()
        {
            // Act
            var profitByMonth = await _orderService.GetProfitByMonthAsync();

            // Assert
            Assert.IsEmpty(profitByMonth);
        }
        
        [Test]
        public async Task CalculateProfitByMonthAsync_ReturnsProfitForCompleteOrdersOnly()
        {
            // Arrange
            await AddOrder(Guid.NewGuid(), _orderStatusCreatedId, DateTime.Parse("2010/01/01"),  1);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/02/01"), 2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/03/01"), 3);
            await AddOrder(Guid.NewGuid(), _orderStatusCreatedId, DateTime.Parse("2010/04/01"), 4);

            // Act
            var profitByMonth = await _orderService.GetProfitByMonthAsync();

            // Assert
            var profitList = profitByMonth.ToList();
            Assert.AreEqual(2, profitList.Count);

            var firstMonth = profitList[0];
            Assert.AreEqual(2, firstMonth.Month);
            Assert.AreEqual(0.2, firstMonth.Profit);
            
            var secondMonth = profitList[1];
            Assert.AreEqual(3, secondMonth.Month);
            Assert.AreEqual(0.3, secondMonth.Profit);
        }
        
        [Test]
        public async Task CalculateProfitByMonthAsync_SumsProfitsCorrectly()
        {
            // Arrange
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2009/01/01"),  2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/01/02"), 2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/03/01"), 2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/03/01"), 2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/03/31"), 2);
            await AddOrder(Guid.NewGuid(), _orderStatusCompletedId, DateTime.Parse("2010/04/01"), 2);

            // Act
            var profitByMonth = await _orderService.GetProfitByMonthAsync();

            // Assert
            var profitList = profitByMonth.ToList();
            Assert.AreEqual(3, profitList.Count);

            var firstMonth = profitList[0];
            Assert.AreEqual(1, firstMonth.Month);
            Assert.AreEqual(0.4, firstMonth.Profit);
            
            var secondMonth = profitList[1];
            Assert.AreEqual(3, secondMonth.Month);
            Assert.AreEqual(0.6, secondMonth.Profit);
            
            var thirdMonth = profitList[2];
            Assert.AreEqual(4, thirdMonth.Month);
            Assert.AreEqual(0.2, thirdMonth.Profit);
        }

        private async Task AddOrder(Guid orderId, Guid statusId, DateTime createdAt, int quantity)
        {
            var orderIdBytes = orderId.ToByteArray();
            _orderContext.Order.Add(new Data.Entities.Order
            {
                Id = orderIdBytes,
                ResellerId = Guid.NewGuid().ToByteArray(),
                CustomerId = Guid.NewGuid().ToByteArray(),
                CreatedDate = createdAt,
                StatusId = statusId.ToByteArray(),
            });

            _orderContext.OrderItem.Add(new OrderItem
            {
                Id = Guid.NewGuid().ToByteArray(),
                OrderId = orderIdBytes,
                ServiceId = _orderServiceEmailId.ToByteArray(),
                ProductId = _orderProductEmailId.ToByteArray(),
                Quantity = quantity
            });

            await _orderContext.SaveChangesAsync();
        }

        private async Task AddReferenceDataAsync(OrderContext orderContext)
        {
            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusCreatedId.ToByteArray(),
                Name = "Created",
            });
            
            orderContext.OrderStatus.Add(new OrderStatus
            {
                Id = _orderStatusCompletedId.ToByteArray(),
                Name = "Completed",
            });

            orderContext.OrderService.Add(new Data.Entities.OrderService
            {
                Id = _orderServiceEmailId.ToByteArray(),
                Name = "Email"
            });

            orderContext.OrderProduct.Add(new OrderProduct
            {
                Id = _orderProductEmailId.ToByteArray(),
                Name = "100GB Mailbox",
                UnitCost = 0.8m,
                UnitPrice = 0.9m,
                ServiceId = _orderServiceEmailId.ToByteArray()
            });
            
            orderContext.OrderProduct.Add(new OrderProduct
            {
                Id = _orderProductEmailEnhancedId.ToByteArray(),
                Name = "200GB Mailbox",
                UnitCost = 1.6m,
                UnitPrice = 1.7m,
                ServiceId = _orderServiceEmailId.ToByteArray()
            });

            await orderContext.SaveChangesAsync();
        }
    }
}
