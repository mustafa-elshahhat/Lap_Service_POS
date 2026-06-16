using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Printing
{
    public class InvoicePrintService : A4PrintBase
    {
        public void PrintSale(Sale sale, List<SaleItem> items)
        {
            PrintFlowDocument(BuildSaleDocument(sale, items), "فاتورة مبيعات - " + sale.InvoiceNumber);
        }

        public void PrintReturn(Return @return, List<ReturnItem> items)
        {
            PrintFlowDocument(BuildReturnDocument(@return, items), "سند مرتجع - " + @return.ReturnNumber);
        }

        public void PrintRepairIntake(RepairOrder order, List<RepairDevice> devices)
        {
            PrintFlowDocument(BuildIntakeDocument(order, devices), "إيصال استلام صيانة - " + order.OrderNumber);
        }

        public void PrintRepairInvoice(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments)
        {
            PrintFlowDocument(BuildRepairInvoiceDocument(order, devices, parts, payments), "فاتورة صيانة - " + order.OrderNumber);
        }

        private FlowDocument BuildSaleDocument(Sale sale, List<SaleItem> items)
        {
            FlowDocument doc = InitializeDocument();
            AddDocumentHeader(doc, "فاتورة مبيعات", "رقم: " + sale.InvoiceNumber);

            var info = new List<(string, string)>
            {
                ("رقم الفاتورة",  sale.InvoiceNumber),
                ("التاريخ والوقت", sale.SaleDate.ToString("yyyy-MM-dd  HH:mm")),
                ("الكاشير",       sale.UserName ?? "مدير النظام"),
                ("طريقة الدفع",  string.IsNullOrEmpty(sale.PaymentMethod) ? "نقدي" : sale.PaymentMethod),
            };
            if (!string.IsNullOrEmpty(sale.CustomerName))
            {
                info.Add(("العميل", sale.CustomerName));
                if (info.Count % 2 != 0) info.Add(("", ""));
            }
            doc.Blocks.Add(CreateInfoGrid(info));

            AddSectionTitle(doc, "بنود الفاتورة");

            var rows = items.Select(i => new[]
            {
                i.ProductName,
                i.Quantity.ToString(),
                Formatting.FormatCurrency(i.UnitFinalPrice),
                Formatting.FormatCurrency(i.Quantity * i.UnitFinalPrice)
            }).ToList();
            doc.Blocks.Add(CreateItemsTable(
                new[] { "المنتج", "الكمية", "سعر الوحدة", "الإجمالي" },
                rows,
                new[] { 3.5, 1.0, 1.5, 1.5 }));

            var totals = new List<(string, string, bool)>
            {
                ("الإجمالي الفرعي", Formatting.FormatCurrency(sale.Subtotal), false)
            };
            if (sale.DiscountAmount > 0)
                totals.Add(("الخصم", "−  " + Formatting.FormatCurrency(sale.DiscountAmount), false));
            if (sale.MarkupAmount > 0)
                totals.Add(("الإضافة", "+  " + Formatting.FormatCurrency(sale.MarkupAmount), false));
            totals.Add(("الإجمالي", Formatting.FormatCurrency(sale.TotalAmount), true));
            totals.Add(("المدفوع",   Formatting.FormatCurrency(sale.PaidAmount), false));
            totals.Add(("المتبقي",   Formatting.FormatCurrency(sale.RemainingAmount), sale.RemainingAmount > 0));
            AddTotalsBox(doc, totals);

            if (!string.IsNullOrEmpty(sale.Notes))
            {
                AddSectionTitle(doc, "ملاحظات");
                doc.Blocks.Add(new Paragraph(new Run(sale.Notes)) { Margin = new Thickness(0, 0, 0, 16) });
            }

            AddSignatureLine(doc);
            AddDocumentFooter(doc, "شكراً لتعاملكم معنا");
            return doc;
        }

        private FlowDocument BuildReturnDocument(Return @return, List<ReturnItem> items)
        {
            FlowDocument doc = InitializeDocument();
            AddDocumentHeader(doc, "سند مرتجع مبيعات", "رقم: " + @return.ReturnNumber);

            var info = new List<(string, string)>
            {
                ("رقم المرتجع",    @return.ReturnNumber),
                ("فاتورة الأصل",  @return.InvoiceNumber ?? "-"),
                ("التاريخ والوقت", @return.ReturnDate.ToString("yyyy-MM-dd  HH:mm")),
                ("الكاشير",        @return.UserName ?? "مدير النظام"),
                ("العميل",         @return.CustomerName ?? "عميل نقدي"),
                ("طريقة الاسترداد", @return.PaymentMethod ?? "نقدي"),
            };
            if (!string.IsNullOrEmpty(@return.Reason))
            {
                info.Add(("سبب الإرجاع", @return.Reason));
                if (info.Count % 2 != 0) info.Add(("", ""));
            }
            doc.Blocks.Add(CreateInfoGrid(info));

            AddSectionTitle(doc, "الأصناف المسترجعة");

            var rows = items.Select(i => new[]
            {
                i.ProductName,
                i.Quantity.ToString(),
                Formatting.FormatCurrency(i.UnitPrice),
                Formatting.FormatCurrency(i.TotalPrice)
            }).ToList();
            doc.Blocks.Add(CreateItemsTable(
                new[] { "المنتج", "الكمية", "سعر الوحدة", "الإجمالي" },
                rows,
                new[] { 3.5, 1.0, 1.5, 1.5 }));

            AddTotalsBox(doc, new List<(string, string, bool)>
            {
                ("إجمالي المرتجع", Formatting.FormatCurrency(@return.TotalAmount), true)
            });

            AddSignatureLine(doc);
            AddDocumentFooter(doc, "شكراً لتعاملكم معنا");
            return doc;
        }

        private FlowDocument BuildIntakeDocument(RepairOrder order, List<RepairDevice> devices)
        {
            FlowDocument doc = InitializeDocument();
            AddDocumentHeader(doc, "إيصال استلام جهاز للصيانة", "رقم الطلب: " + order.OrderNumber);

            var info = new List<(string, string)>
            {
                ("رقم الطلب",        order.OrderNumber),
                ("تاريخ الاستلام",   order.IntakeDate.ToString("yyyy-MM-dd  HH:mm")),
                ("اسم العميل",        order.CustomerName ?? "-"),
                ("هاتف العميل",       order.CustomerPhone ?? "-"),
                ("الفني المسؤول",     order.TechnicianName ?? "-"),
                ("التسليم المتوقع",   order.ExpectedDelivery?.ToString("yyyy-MM-dd") ?? "-"),
            };
            doc.Blocks.Add(CreateInfoGrid(info));

            AddSectionTitle(doc, "الأجهزة المستلمة");

            var rows = devices.Select(d => new[]
            {
                d.DisplayName,
                string.IsNullOrEmpty(d.SerialNumber) ? "-" : d.SerialNumber,
                string.IsNullOrEmpty(d.Condition)    ? "-" : d.Condition,
                d.ReportedIssue ?? "-",
                string.IsNullOrEmpty(d.Accessories)  ? "-" : d.Accessories
            }).ToList();
            doc.Blocks.Add(CreateItemsTable(
                new[] { "الجهاز", "السيريال", "الحالة", "المشكلة", "الملحقات" },
                rows,
                new[] { 2.0, 1.5, 1.0, 2.5, 1.5 }));

            if (!string.IsNullOrEmpty(order.Notes))
            {
                AddSectionTitle(doc, "ملاحظات");
                doc.Blocks.Add(new Paragraph(new Run(order.Notes)) { Margin = new Thickness(0, 0, 0, 16) });
            }

            AddSignatureLine(doc);
            AddDocumentFooter(doc, "شكراً لثقتكم بنا  —  يُرجى الاحتفاظ بهذا الإيصال");
            return doc;
        }

        private FlowDocument BuildRepairInvoiceDocument(
            RepairOrder order, List<RepairDevice> devices,
            List<RepairPart> parts, List<RepairPayment> payments)
        {
            FlowDocument doc = InitializeDocument();
            AddDocumentHeader(doc, "فاتورة صيانة", "رقم الطلب: " + order.OrderNumber);

            var info = new List<(string, string)>
            {
                ("رقم الطلب",      order.OrderNumber),
                ("تاريخ الاستلام", order.IntakeDate.ToString("yyyy-MM-dd")),
                ("اسم العميل",      order.CustomerName ?? "-"),
                ("هاتف العميل",     order.CustomerPhone ?? "-"),
                ("الفني المسؤول",   order.TechnicianName ?? "-"),
                ("تاريخ التسليم",   order.DeliveryDate?.ToString("yyyy-MM-dd") ?? "-"),
            };
            doc.Blocks.Add(CreateInfoGrid(info));

            AddSectionTitle(doc, "الأجهزة والخدمات");

            foreach (var device in devices)
            {
                Border card = new Border
                {
                    Background      = new SolidColorBrush(CardBgColor),
                    BorderBrush     = new SolidColorBrush(SubHeaderColor),
                    BorderThickness = new Thickness(0, 0, 4, 2),
                    CornerRadius    = new CornerRadius(4),
                    Padding  = new Thickness(12, 8, 12, 8),
                    Margin   = new Thickness(0, 0, 0, 8)
                };
                StackPanel cardContent = new StackPanel();
                cardContent.Children.Add(new TextBlock(new Run("▪  " + device.DisplayName))
                {
                    FontWeight = FontWeights.Bold, FontSize = 13,
                    Margin = new Thickness(0, 0, 0, 4)
                });
                if (!string.IsNullOrEmpty(device.ReportedIssue))
                    cardContent.Children.Add(new TextBlock(new Run("المشكلة: " + device.ReportedIssue))
                    { FontSize = 11, Foreground = Brushes.DimGray });
                if (!string.IsNullOrEmpty(device.RepairNotes))
                    cardContent.Children.Add(new TextBlock(new Run("الإصلاح: " + device.RepairNotes))
                    { FontSize = 11, Foreground = Brushes.DimGray });
                card.Child = cardContent;
                doc.Blocks.Add(new BlockUIContainer(card));

                var deviceParts = parts.Where(p => p.DeviceId == device.Id).ToList();
                if (deviceParts.Count > 0)
                {
                    var pRows = deviceParts.Select(p => new[]
                    {
                        p.PartName,
                        p.Quantity.ToString(),
                        Formatting.FormatCurrency(p.UnitCost),
                        Formatting.FormatCurrency(p.TotalCost)
                    }).ToList();
                    doc.Blocks.Add(CreateItemsTable(
                        new[] { "قطعة الغيار", "الكمية", "سعر الوحدة", "الإجمالي" },
                        pRows,
                        new[] { 3.0, 1.0, 1.5, 1.5 }));
                }

                if (device.LaborCost > 0)
                    doc.Blocks.Add(new Paragraph(new Run("أجر العمل:  " + Formatting.FormatCurrency(device.LaborCost)))
                    {
                        TextAlignment = TextAlignment.Right,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = new SolidColorBrush(PrimaryColor),
                        Margin = new Thickness(0, 0, 0, 12)
                    });
            }

            AddSectionTitle(doc, "ملخص الفاتورة");
            var totals = new List<(string, string, bool)>
            {
                ("إجمالي الفاتورة", Formatting.FormatCurrency(order.TotalAmount), true),
                ("المدفوع",          Formatting.FormatCurrency(order.PaidAmount), false),
                ("المتبقي",          Formatting.FormatCurrency(order.RemainingAmount), order.RemainingAmount > 0),
            };
            AddTotalsBox(doc, totals);

            if (payments != null && payments.Count > 0)
            {
                AddSectionTitle(doc, "سجل المدفوعات");
                var pRows = payments.Select(p => new[]
                {
                    p.PaymentDate.ToString("yyyy-MM-dd  HH:mm"),
                    p.PaymentMethod ?? "نقدي",
                    Formatting.FormatCurrency(p.Amount),
                    p.Notes ?? ""
                }).ToList();
                doc.Blocks.Add(CreateItemsTable(
                    new[] { "التاريخ", "طريقة الدفع", "المبلغ", "ملاحظات" },
                    pRows,
                    new[] { 2.0, 1.5, 1.5, 2.0 }));
            }

            if (!string.IsNullOrEmpty(order.Notes))
            {
                AddSectionTitle(doc, "ملاحظات");
                doc.Blocks.Add(new Paragraph(new Run(order.Notes)) { Margin = new Thickness(0, 0, 0, 16) });
            }

            AddSignatureLine(doc);
            AddDocumentFooter(doc, "شكراً لثقتكم بنا");
            return doc;
        }
    }
}
