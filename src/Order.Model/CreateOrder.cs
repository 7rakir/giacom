using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Order.Model;

public class CreateOrder : IValidatableObject
{
	[Required]
	public Guid ResellerId { get; init; }
	
	[Required]
	public Guid CustomerId { get; init; }
	
	[Required]
	[Length(1, int.MaxValue)]
	public CreateOrderItem[] Items { get; init; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		var productIds = Items.Select(i => i.ProductId).ToArray();
		if (productIds.Distinct().Count() != productIds.Length)
		{
			yield return new ValidationResult($"Every Order item must be for a unique product.", [nameof(Items)]);
		}
	}
}