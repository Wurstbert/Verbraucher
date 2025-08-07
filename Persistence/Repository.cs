using Microsoft.EntityFrameworkCore;
using Verbraucher.Entities;

namespace Verbraucher.Persistence;

public class Repository
{
    private readonly IDbContextFactory<VerbraucherContext> contextFactory;
    public Repository(IDbContextFactory<VerbraucherContext> contextFactory) 
    {
        this.contextFactory = contextFactory;
    }

    public int SaveInvoiceLineItems(List<InvoiceLineItem> invoiceLineItems)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            context.InvoiceLineItems.AddRange(invoiceLineItems);
            return context.SaveChanges();
        }
    }

    public List<Product> GetProducts()
    {
        using (var context = contextFactory.CreateDbContext())
        {
            return context.Products.ToList();
        }
    }

    public Product? GetProduct(Guid id)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            return context.Products.Find(id);
        }
    }

    public Product? GetProductByName(string name)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            return context.Products.Where(product => product.Name == name).FirstOrDefault();
        }
    }
    public int SaveProducts(List<Product> products)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            context.Products.AddRange(products);
            return context.SaveChanges();
        }
    }

    public List<InvoiceLineItem> GetInvoiceLineItems()
    {
        using (var context = contextFactory.CreateDbContext())
        {
            return context.InvoiceLineItems.ToList();
        }
    }

    public void SaveToTextFile(string filePath, List<string> productStrings)
    {
        var directoryName = Path.GetDirectoryName(filePath);
        var fileName = Path.GetFileNameWithoutExtension(filePath);

        var productStringsFullPath = Path.Combine(directoryName, $"{fileName}_productStrings.txt");

        File.WriteAllLines(productStringsFullPath, productStrings);
    }

    public int SaveInvoices(List<Invoice> invoices)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            context.Invoices.AddRange(invoices);
            return context.SaveChanges();
        }
    }
    public Invoice? GetInvoice(Guid id)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            return context.Invoices.Find(id);
        }
    }

    public void SetInvoiceLineItemDebtor(Guid itemId, Debtor? debtor)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            var product = context.InvoiceLineItems.Find(itemId);
            if (product != null)
            {
                product.Responsible = debtor?.ToString();
                context.SaveChanges();
            }
        }
    }

    public void SetProductDefaultDebtor(Guid productId, Debtor? debtor)
    {
        using (var context = contextFactory.CreateDbContext())
        {
            var product = context.Products.Find(productId);
            if (product != null)
            {
                product.DefaultDebtor = debtor?.ToString();
                context.SaveChanges();
            }
        }
    }

}
