using System;
using System.Collections.Generic;

namespace Verbraucher.Entities;

public partial class InvoiceLineItem
{
    public Guid Id { get; set; }

    public Guid? InvoiceId { get; set; }

    public Guid ProductId { get; set; }

    public float? Quantity { get; set; }

    public string? Unit { get; set; }

    public decimal? PriceInEuro { get; set; }

    public decimal? DiscountInEuro { get; set; }

    public string? Responsible { get; set; }

    public bool? ContainsNewProduct { get; set; }

    public bool? Paid { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Invoice? Invoice { get; set; }

    public virtual Product Product { get; set; } = null!;
}
