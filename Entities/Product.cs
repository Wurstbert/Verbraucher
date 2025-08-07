using System;
using System.Collections.Generic;

namespace Verbraucher.Entities;

public partial class Product
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string? DefaultDebtor { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<InvoiceLineItem> InvoiceLineItems { get; set; } = new List<InvoiceLineItem>();
}
