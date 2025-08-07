using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf;
using System.Text;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Verbraucher.Entities;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Verbraucher.Persistence;

namespace Verbraucher.Services;

public class PdfService
{
    private readonly Repository repository;

    public PdfService(Repository repository)
    {
        this.repository = repository;
    }

    public Guid? Parse(string filePath)
    {
        var invoiceId = Guid.Parse(Path.GetFileNameWithoutExtension(filePath));
        if (repository.GetInvoice(invoiceId) != null)
        {
            return null;
        }

        string pdfString = GetPdfString(filePath);
        repository.SaveInvoices(new List<Invoice> { new Invoice { Id = invoiceId, CreatedAt = DateTime.Now, IssuedAt = GetIssuedAt(pdfString) } });

        var invoiceBlocks = GetInvoiceBlocks(pdfString);
        repository.SaveToTextFile(filePath, invoiceBlocks);

        var invoiceLineItems = ConvertToInvoiceLineItems(invoiceId, invoiceBlocks);
        repository.SaveInvoiceLineItems(invoiceLineItems);

        return invoiceId;
    }
    
    public DateTime? GetIssuedAt(string pdfString)
    {
        DateTime issuedAt;
        var dateLine = Regex.Match(pdfString, @"\d{2}\.\d{2}\.\d{4}.*?T‑ID").Value;
        var dateTimeString = $"{dateLine.Split()[0]} {dateLine.Split().Where(x => !string.IsNullOrWhiteSpace(x)).ToList()[1]}";
        if (DateTime.TryParse(dateTimeString, out issuedAt))
        {
            return issuedAt;
        }

        return null;
    }

    public List<string> GetInvoiceBlocks(string pdfString)
    {
        var invoiceSectionString = pdfString.Split("Summe in €")[1].Split("Zwischensumme")[0];
        var invoiceBlocks = new List<string>();
        string[] invoiceLines = invoiceSectionString.Split("\n");

        var currentBlock = string.Empty;

        foreach (string line in invoiceLines)
        {
            var trimmedLine = line.Trim();

            if (Regex.IsMatch(line.Replace('‑', '-'), @"(-\d+,\d+)$"))
            {
                invoiceBlocks[invoiceBlocks.Count - 1] = invoiceBlocks[invoiceBlocks.Count - 1] += trimmedLine;
                continue;
            }

            if (trimmedLine != string.Empty)
            {
                currentBlock += trimmedLine + "\n";
            }

            if (Regex.IsMatch(line, @"(\d+,\d+)([A-Za-z])$"))
            {
                invoiceBlocks.Add(currentBlock);
                currentBlock = string.Empty;
            }
        }

        return invoiceBlocks;
    }

    private static string GetPdfString(string filePath)
    {
        StringBuilder pdfStringBuilder = new StringBuilder();

        var pdfDocument = new PdfDocument(new PdfReader(filePath));
        var strategy = new LocationTextExtractionStrategy();

        for (int i = 1; i <= pdfDocument.GetNumberOfPages(); ++i)
        {
            var page = pdfDocument.GetPage(i);
            string text = PdfTextExtractor.GetTextFromPage(page, strategy);
            pdfStringBuilder.Append(text);
        }

        pdfDocument.Close();
        var pdfString = pdfStringBuilder.ToString();
        return pdfString;
    }

    private List<InvoiceLineItem> ConvertToInvoiceLineItems(Guid invoiceId, List<string> invoiceBlocks)
    {
        var invoiceLineItems = new List<InvoiceLineItem>();

        foreach (var block in invoiceBlocks)
        {
            var invoiceLineItem = new InvoiceLineItem();
            invoiceLineItem.InvoiceId = invoiceId;
            invoiceLineItem.Id = Guid.CreateVersion7();
            invoiceLineItem.CreatedAt = DateTime.Now;
            string? invoiceLineItemName = null;

            foreach (var line in block.Split("\n"))
            {
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }

                string pricePattern = @"(\d+,\d+)([A-Za-z])$";
                Match priceMatch = Regex.Match(line, pricePattern);
                decimal price;
                if (priceMatch.Success && decimal.TryParse(priceMatch.Groups[1].Value, NumberStyles.Number, CultureInfo.CreateSpecificCulture("de-DE"), out price))
                {
                    invoiceLineItem.PriceInEuro = price; // Extract the decimal number
                }
                else
                {
                    // Fehler beim Lesen des Preises
                }

                string dicountPattern = @"(-\d+,\d+)$";
                Match dicountMatch = Regex.Match(line.Replace('‑', '-'), dicountPattern);
                decimal discount;
                if (dicountMatch.Success && decimal.TryParse(dicountMatch.Captures.First().Value, NumberStyles.Number, CultureInfo.CreateSpecificCulture("de-DE"), out discount))
                {
                    invoiceLineItem.DiscountInEuro = discount;
                }
                else
                {
                    // Fehler beim Lesen des Rabatts
                }

                // Suche nach "1 x", "2 x", ...
                string quantityPattern = @"^(\d+)\s+x";
                Match quantityMatch = Regex.Match(line.Replace('‑', '-'), quantityPattern);
                if (quantityMatch.Success)
                {
                    invoiceLineItem.Quantity = int.Parse(quantityMatch.Captures.First().Value.Split().First());
                    invoiceLineItem.Unit = "Stück";

                    string lineCopy = line;

                    if (priceMatch.Success)
                    {
                        lineCopy = lineCopy.Remove(priceMatch.Index);
                    }

                    try
                    {
                        invoiceLineItemName = lineCopy.Split(quantityMatch.Captures.First().Value)[1].Trim();
                    }
                    catch (Exception e)
                    {
                        // Fehler beim Einlesen des Namens
                    }
                }

                if (!priceMatch.Success && !dicountMatch.Success && !quantityMatch.Success)
                {
                    invoiceLineItemName = line.Trim();
                }

                // Suche nach "2 St. x", "3 St. x" ...
                // Da diese genaueren Angaben in der Konsumrechnung eine Zeile später erscheinen, überschreiben wir die Felder einfach
                quantityPattern = @"^(\d+)\s+St\.\s+x";
                if (Regex.IsMatch(line, quantityPattern))
                {
                    invoiceLineItem.Quantity = int.Parse(Regex.Match(line, quantityPattern).Captures.First().Value.Split().First());
                    invoiceLineItem.Unit = "Stück";
                }

                // Suche nach "0,272 kg x", "0,060 kg x", "1,500 kg x", ...
                // Da diese genaueren Angaben in der Konsumrechnung eine Zeile später erscheinen, überschreiben wir die Felder einfach
                quantityPattern = @"^(\d+,\d+)\s+kg\s+x";
                if (Regex.IsMatch(line, quantityPattern))
                {
                    float quantity;
                    if (float.TryParse(Regex.Match(line, quantityPattern).Captures.First().Value.Split().First(), NumberStyles.Number, CultureInfo.CreateSpecificCulture("de-DE"), out quantity))
                    {
                        invoiceLineItem.Quantity = quantity;
                    }
                    else
                    {
                        // Fehler beim Lesen der Menge
                    }

                    invoiceLineItem.Unit = "kg";
                }
            }

            var product = repository.GetProductByName(invoiceLineItemName);

            // Product erstellen, falls es neu ist
            if (product == null)
            {
                product = new Product()
                {
                    Id = Guid.CreateVersion7(),
                    Name = invoiceLineItemName,
                    CreatedAt = DateTime.Now
                };

                repository.SaveProducts(new List<Product>{ product });
                invoiceLineItem.ContainsNewProduct = true;
            }

            invoiceLineItem.ProductId = product.Id;
            invoiceLineItem.Responsible = product.DefaultDebtor;

            invoiceLineItems.Add(invoiceLineItem);
        }

        return invoiceLineItems;
    }
}

