using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Printing
{
    public class SupplierStatementPrintService : A4PrintBase
    {
        public void Print(string supplierName, IEnumerable<Dictionary<string, object>> transactions, Dictionary<int, List<SupplierPurchaseItem>> transactionItems)
        {
            var transactionList = (transactions ?? Enumerable.Empty<Dictionary<string, object>>()).ToList();
            transactionItems = transactionItems ?? new Dictionary<int, List<SupplierPurchaseItem>>();

            var doc = InitializeDocument();
            AddDocumentHeader(doc, "كشف حساب مورد", supplierName ?? "-");

            foreach (var transaction in transactionList)
            {
                int transactionId = SafeConvert.ToInt(transaction.ContainsKey("id") ? transaction["id"] : null);
                string type = SafeConvert.ToString(transaction.ContainsKey("transaction_type") ? transaction["transaction_type"] : null);
                string typeAr = transaction.ContainsKey("transaction_type_ar")
                    ? SafeConvert.ToString(transaction["transaction_type_ar"])
                    : type == "purchase" ? "مشتريات" : "دفعة";

                var info = new List<(string label, string value)>
                {
                    ("#", transactionId.ToString()),
                    ("التاريخ", Formatting.FormatDate(transaction.ContainsKey("transaction_date") ? transaction["transaction_date"] : null, true)),
                    ("النوع", typeAr),
                    ("المبلغ", Formatting.FormatCurrency(SafeConvert.ToDecimal(transaction.ContainsKey("amount") ? transaction["amount"] : null))),
                    ("المدفوع", Formatting.FormatCurrency(SafeConvert.ToDecimal(transaction.ContainsKey("paid_amount") ? transaction["paid_amount"] : null))),
                    ("الرصيد بعد", Formatting.FormatCurrency(SafeConvert.ToDecimal(transaction.ContainsKey("balance_after") ? transaction["balance_after"] : null)))
                };

                var block = new Section { Margin = new Thickness(0, 0, 0, 12) };
                block.Blocks.Add(CreateInfoGrid(info));

                if (type == "purchase" && transactionItems.TryGetValue(transactionId, out var items) && items.Count > 0)
                {
                    var rows = items.Select(i => new[]
                    {
                        i.ProductName ?? "",
                        i.Quantity.ToString(),
                        Formatting.FormatCurrency(i.UnitPurchasePrice),
                        Formatting.FormatCurrency(i.LineTotal)
                    }).ToList();

                    block.Blocks.Add(new Paragraph(new Run("أصناف الفاتورة"))
                    {
                        FontSize = 13,
                        FontWeight = FontWeights.Bold,
                        Foreground = new SolidColorBrush(PrimaryColor),
                        Margin = new Thickness(0, 2, 0, 6)
                    });
                    block.Blocks.Add(CreateItemsTable(
                        new[] { "اسم المنتج", "الكمية", "سعر الشراء", "الإجمالي" },
                        rows,
                        new[] { 2.2, 0.8, 1.2, 1.2 }));
                }

                doc.Blocks.Add(block);
            }

            if (transactionList.Count == 0)
            {
                doc.Blocks.Add(new Paragraph(new Run("لا توجد حركات لهذا المورد"))
                {
                    TextAlignment = TextAlignment.Center,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(0, 24, 0, 24)
                });
            }

            AddDocumentFooter(doc, "--- نهاية كشف الحساب ---");
            PrintFlowDocument(doc, "كشف حساب مورد");
        }
    }
}
