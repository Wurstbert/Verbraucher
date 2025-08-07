using System;
using System.Collections.Generic;

namespace Verbraucher.Entities;

public partial class Invoice
{
    public Guid Id { get; set; }

    public DateTime? IssuedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<InvoiceLineItem> InvoiceLineItems { get; set; } = new List<InvoiceLineItem>();
}
