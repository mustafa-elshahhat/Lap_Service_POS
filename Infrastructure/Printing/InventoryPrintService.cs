using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Documents;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Printing
{
    public class InventoryPrintService : A4PrintBase
    {
        public void Print(List<Product> products, Dictionary<string, object> totals)
        {
            products = products ?? new List<Product>();
            totals = totals ?? new Dictionary<string, object>();

            var doc = InitializeDocument();
            AddDocumentHeader(doc, "جرد المخزون", $"عدد الأصناف: {products.Count} - بتاريخ {DateTime.Now:yyyy-MM-dd}");

            string[] headers =
            {
                "الكود", "اسم المنتج", "الفئة", "الكمية", "سعر الشراء", "سعر البيع", "إجمالي الشراء", "إجمالي البيع"
            };

            var rows = products.Select(p => new[]
            {
                p.Code ?? "",
                p.Name ?? "",
                p.Category ?? "",
                p.Quantity.ToString(),
                Formatting.FormatCurrency(p.PurchasePrice),
                Formatting.FormatCurrency(p.SellingPrice),
                Formatting.FormatCurrency(p.Quantity * p.PurchasePrice),
                Formatting.FormatCurrency(p.Quantity * p.SellingPrice)
            }).ToList();

            doc.Blocks.Add(CreateItemsTable(headers, rows, new[] { 1.0, 2.0, 1.2, 0.8, 1.1, 1.1, 1.3, 1.3 }));

            decimal purchaseValue = GetDecimal(totals, "purchase_value");
            decimal sellingValue = GetDecimal(totals, "selling_value");
            AddTotalsBox(doc, new List<(string label, string value, bool highlight)>
            {
                ("إجمالي عدد الأصناف", GetDecimal(totals, "total_products").ToString("N0"), false),
                ("إجمالي الكمية", GetDecimal(totals, "total_quantity").ToString("N0"), false),
                ("إجمالي تكلفة المخزون", Formatting.FormatCurrency(purchaseValue), true),
                ("إجمالي قيمة البيع", Formatting.FormatCurrency(sellingValue), true),
                ("الربح المتوقع", Formatting.FormatCurrency(sellingValue - purchaseValue), true)
            });

            AddDocumentFooter(doc, "--- نهاية الجرد ---");
            PrintFlowDocument(doc, "جرد المخزون");
        }

        private static decimal GetDecimal(Dictionary<string, object> values, string key)
        {
            return values.ContainsKey(key) ? SafeConvert.ToDecimal(values[key]) : 0m;
        }
    }
}
