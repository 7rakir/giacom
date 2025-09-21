using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Order.Service;
using System;
using System.Threading.Tasks;
using Order.Model;

namespace OrderService.WebAPI.Controllers
{
    [ApiController]
    [Route("orders")]
    public class OrderController(IOrderService orderService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> Get()
        {
            var orders = await orderService.GetOrdersAsync();
            return Ok(orders);
        }
        
        [HttpPost("byStatus")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByStatus([FromBody] string status)
        {
            var orders = await orderService.GetOrdersByStatusAsync(status);
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderById(Guid orderId)
        {
            var order = await orderService.GetOrderByIdAsync(orderId);
            if (order != null)
            {
                return Ok(order);
            }

            return NotFound();
        }

        [HttpPut("{orderId}/status")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateOrderStatus(Guid orderId, [FromBody] string status)
        {
            var order = await orderService.UpdateOrderStatusAsync(orderId, status);
            if (order != null)
            {
                return Ok(order);
            }

            return NotFound();
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrder createOrder)
        {
            var orderId = await orderService.CreateOrderAsync(createOrder);
            return CreatedAtAction(nameof(GetOrderById), new { orderId }, orderId);
        }

        [HttpGet("profitByMonth")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProfitByMonth()
        {
            var profit = await orderService.GetProfitByMonthAsync();
            return Ok(profit);
        }
    }
}
