using System;
using System.ComponentModel.DataAnnotations;

namespace Order.Model;

public class CreateOrderItem
{
	[Required]
	public Guid ServiceId { get; init; }
	
	[Required]
	public Guid ProductId { get; init; }
	
	[Required]
	[Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
	public int Quantity { get; init; }
}