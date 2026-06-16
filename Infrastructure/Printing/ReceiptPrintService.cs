using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Presentation;
using AlJohary.ServiceHub.Shared.Helpers;
using System.Linq;

using System.Printing;

namespace AlJohary.ServiceHub.Infrastructure.Printing
{
    public class ReceiptPrintService
    {
        public void PrintSale(Sale sale, List<SaleItem> items)
        {
            var doc = CreateSaleDocument(sale, items);
            PrintDocument(doc, "SaleInvoice_" + sale.InvoiceNumber);
        }

        public void PrintReturn(Return @return, List<ReturnItem> items)
        {
            var doc = CreateReturnDocument(@return, items);
            PrintDocument(doc, "ReturnReceipt_" + @return.ReturnNumber);
        }

        public void PrintRepairIntake(RepairOrder order, List<RepairDevice> devices)
        {
            var doc = CreateRepairIntakeDocument(order, devices);
            PrintDocument(doc, "RepairIntake_" + order.OrderNumber);
        }

        public void PrintRepairInvoice(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments)
        {
            var doc = CreateRepairInvoiceDocument(order, devices, parts, payments);
            PrintDocument(doc, "RepairInvoice_" + order.OrderNumber, 1120);
        }

        private void PrintDocument(FlowDocument doc, string jobName, double? fixedHeight = null)
        {
            try
            {
                PrintDialog printDialog = new PrintDialog();
                if (printDialog.ShowDialog() == true)
                {

                    double pageWidth = (doc.PageWidth > 1 && doc.PageWidth < 500) ? doc.PageWidth : 300;
                    Thickness padding = doc.PagePadding;
                    if (padding.Left + padding.Right >= pageWidth) padding = new Thickness(5);

                    double finalHeight;
                    if (fixedHeight.HasValue)
                    {
                         finalHeight = fixedHeight.Value;
                         doc.PageHeight = finalHeight;
                    }
                    else
                    {
                        doc.PageHeight = 16000;
                        doc.PageWidth = pageWidth;
                        doc.PagePadding = padding;
                        doc.ColumnGap = 0;
                        doc.ColumnWidth = pageWidth - (padding.Left + padding.Right);
                        doc.FlowDirection = FlowDirection.RightToLeft;

                        IDocumentPaginatorSource idps = doc;
                        var pT = idps.DocumentPaginator;
                        pT.PageSize = new Size(pageWidth, 16000);
                        
                        try {
                            if (pT.PageCount > 0)
                            {
                                DocumentPage page = pT.GetPage(0);

                                double contentH = page.ContentBox.Bottom + padding.Bottom + 50; 

                                if (contentH < 400) contentH = 400;

                                finalHeight = contentH;
                            }
                            else
                            {
                                finalHeight = 500; 
                            }
                        }
                        catch 
                        {
                            finalHeight = 1000; 
                        }

                        doc.PageHeight = finalHeight;
                        
                        var checkPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                        checkPaginator.PageSize = new Size(pageWidth, finalHeight);

                        int attempts = 0;
                        while(checkPaginator.PageCount > 1 && attempts < 15)
                        {
                            finalHeight += 150;
                            doc.PageHeight = finalHeight;
                            checkPaginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                            checkPaginator.PageSize = new Size(pageWidth, finalHeight);
                            attempts++;
                        }
                    }

                    doc.PageWidth = pageWidth;
                    doc.PageHeight = finalHeight;
                    doc.PagePadding = padding;
                    doc.ColumnGap = 0;
                    doc.ColumnWidth = pageWidth - (padding.Left + padding.Right);
                    doc.FlowDirection = FlowDirection.RightToLeft;

                    IDocumentPaginatorSource idpSource = doc;
                    var paginator = idpSource.DocumentPaginator;
                    paginator.PageSize = new Size(pageWidth, finalHeight);

                    try {
                        printDialog.PrintTicket.PageMediaSize = new System.Printing.PageMediaSize(pageWidth, finalHeight);
                    } catch {}

                    printDialog.PrintDocument(paginator, jobName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الطباعة: " + ex.Message, "خطأ طباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private FlowDocument CreateSaleDocument(Sale sale, List<SaleItem> items)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Courier New"); 
            doc.FontSize = 10; 
            doc.FlowDirection = FlowDirection.RightToLeft; 
            
            doc.IsOptimalParagraphEnabled = false;
            doc.IsHyphenationEnabled = false;
            
            Section mainSection = new Section();

            string shopName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");
            string phonesText = GetPhonesText();
            string typeDisplay = "بيع / نقدي";

            bool logoLoaded = false;
            try 
            {
                 var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                 bitmap.BeginInit(); 
                 bitmap.UriSource = new Uri("pack://application:,,,/Presentation/Resources/Images/logo.png"); 
                 bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; 
                 bitmap.EndInit();
                 
                 var logoImg = new Image() { Source = bitmap, Width = 80, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,2), FlowDirection = FlowDirection.LeftToRight };
                 mainSection.Blocks.Add(new BlockUIContainer(logoImg));
                 logoLoaded = true;
            } 
            catch { }

            if (!logoLoaded)
            {
                mainSection.Blocks.Add(new Paragraph(new Run(shopName)) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 5) });
            }
            if (!string.IsNullOrWhiteSpace(phonesText))
            {
                mainSection.Blocks.Add(new Paragraph(new Run(phonesText)) { FontSize = 9, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 3), FlowDirection = FlowDirection.LeftToRight });
            }
            mainSection.Blocks.Add(CreateSeparator());

            var metadataItems = new List<(string, string)>
            {
                ("رقم الفاتورة", sale.InvoiceNumber),
                ("التاريخ", sale.SaleDate.ToString("yyyy-MM-dd")),
                ("الوقت", sale.SaleDate.ToString("HH:mm")),
                ("الكاشير", sale.UserName ?? "مدير النظام"),
                ("نوع الفاتورة", typeDisplay)
            };
            mainSection.Blocks.Add(CreateAlignedInfoTable(metadataItems));
            mainSection.Blocks.Add(CreateSeparator());

            Table table = new Table();
            table.CellSpacing = 0;
            table.Columns.Add(new TableColumn() { Width = new GridLength(140) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(35) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(50) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(65) });

            TableRowGroup group = new TableRowGroup();
            TableRow headerRow = new TableRow();
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("الصنف")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }));
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("كمية")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("سعر")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
            headerRow.Cells.Add(new TableCell(new Paragraph(new Run("إجمالي")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black }); 
            group.Rows.Add(headerRow);

            TableRow sepRow = new TableRow();
            sepRow.Cells.Add(new TableCell(CreateSeparator()) { ColumnSpan = 4 });
            group.Rows.Add(sepRow);

            foreach (var item in items)
            {
                TableRow row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductName)) { TextAlignment = TextAlignment.Center }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString())) { TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
                row.Cells.Add(new TableCell(new Paragraph(new Run(Formatting.FormatNumber(item.UnitFinalPrice))) { TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
                row.Cells.Add(new TableCell(new Paragraph(new Run(Formatting.FormatNumber(item.Quantity * item.UnitFinalPrice))) { TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
                group.Rows.Add(row);
            }

            table.RowGroups.Add(group);
            mainSection.Blocks.Add(table);
            mainSection.Blocks.Add(CreateSeparator());

            var totalItems = new List<(string, string)>
            {
                ("إجمالي الفاتورة", Formatting.FormatNumber(sale.TotalAmount)),
                ("المدفوع", Formatting.FormatNumber(sale.PaidAmount)),
                ("المتبقي", Formatting.FormatNumber(sale.RemainingAmount))
            };
            mainSection.Blocks.Add(CreateAlignedInfoTable(totalItems, true));
            mainSection.Blocks.Add(CreateSeparator());

            // Show a blank/unknown method literally rather than silently substituting "نقدي" — a genuine
            // blank should be visible, not masked as cash.
            string pMethod = string.IsNullOrWhiteSpace(sale.PaymentMethod) ? "غير محدد" : sale.PaymentMethod;
            mainSection.Blocks.Add(new Paragraph(new Run($"طريقة الدفع : {pMethod}")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 1, 0, 1) });
            mainSection.Blocks.Add(CreateSeparator());

            mainSection.Blocks.Add(new Paragraph(new Run("شكراً لتعاملكم معنا")) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 0, 0) });
            mainSection.Blocks.Add(CreateSeparator());

            doc.Blocks.Add(mainSection);
            return doc;
        }

        private Paragraph CreateSeparator()
        {
            return new Paragraph(new Run("────────────────────────────────")) 
            { 
                TextAlignment = TextAlignment.Center, 
                Margin = new Thickness(0, 1, 0, 1),
                FontSize = 10 
            };
        }

        private Paragraph CreateFieldLine(string label, string value)
        {
            Paragraph p = new Paragraph();
            p.Margin = new Thickness(0);
            p.TextAlignment = TextAlignment.Left; 

            p.Inlines.Add(new Run(label + " : "));

            Span valueSpan = new Span(new Run(value));
            valueSpan.FlowDirection = FlowDirection.LeftToRight;
            
            p.Inlines.Add(valueSpan);
            return p;
        }
        
        private Table CreateAlignedInfoTable(List<(string label, string value)> items, bool bold = false)
        {
            Table table = new Table();
            table.CellSpacing = 0;
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) }); 
            table.Columns.Add(new TableColumn() { Width = new GridLength(130) }); 
            table.Columns.Add(new TableColumn() { Width = new GridLength(130) }); 
            table.Columns.Add(new TableColumn() { Width = new GridLength(1, GridUnitType.Star) });
            
            TableRowGroup group = new TableRowGroup();
            foreach (var item in items)
            {
                TableRow row = new TableRow();
                row.Cells.Add(new TableCell()); 
                
                var lPara = new Paragraph(new Run(item.label)) { TextAlignment = TextAlignment.Right, Margin = new Thickness(0) };
                if (bold) lPara.FontWeight = FontWeights.Bold;
                row.Cells.Add(new TableCell(lPara));
                
                var vPara = new Paragraph(new Run(" : " + item.value)) { TextAlignment = TextAlignment.Left, Margin = new Thickness(0) };
                if (bold) vPara.FontWeight = FontWeights.Bold;
                row.Cells.Add(new TableCell(vPara));
                
                row.Cells.Add(new TableCell()); 
                group.Rows.Add(row);
            }
            table.RowGroups.Add(group);
            return table;
        }

        private FlowDocument CreateReturnDocument(Return @return, List<ReturnItem> items)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Courier New");
            doc.FontSize = 10;
            doc.FlowDirection = FlowDirection.RightToLeft; 
            doc.IsOptimalParagraphEnabled = true;
            doc.IsHyphenationEnabled = false;

            Section mainSection = new Section();

            string shopName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");
            string phonesText = GetPhonesText();
            
            bool logoLoaded = false;
            try 
            {
                 var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                 bitmap.BeginInit(); 
                 bitmap.UriSource = new Uri("pack://application:,,,/Presentation/Resources/Images/logo.png"); 
                 bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; 
                 bitmap.EndInit();
                 
                 var logoImg = new Image() { Source = bitmap, Width = 80, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0,0,0,2), FlowDirection = FlowDirection.LeftToRight };
                 mainSection.Blocks.Add(new BlockUIContainer(logoImg));
                 logoLoaded = true;
            } 
            catch { }

            if (!logoLoaded)
            {
                mainSection.Blocks.Add(new Paragraph(new Run(shopName)) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 5) });
            }
            if (!string.IsNullOrWhiteSpace(phonesText))
            {
                mainSection.Blocks.Add(new Paragraph(new Run(phonesText)) { FontSize = 9, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 3), FlowDirection = FlowDirection.LeftToRight });
            }
            mainSection.Blocks.Add(CreateSeparator());

            var metadataItems = new List<(string, string)>();
            metadataItems.Add(("رقم المرتجع", @return.ReturnNumber));
            if (!string.IsNullOrEmpty(@return.InvoiceNumber))
                 metadataItems.Add(("فاتورة الأصل", @return.InvoiceNumber));
            
            metadataItems.Add(("التاريخ", @return.ReturnDate.ToString("yyyy-MM-dd")));
            metadataItems.Add(("الوقت", @return.ReturnDate.ToString("HH:mm")));
            metadataItems.Add(("الكاشير", @return.UserName ?? "مدير النظام"));

            if (!string.IsNullOrEmpty(@return.CustomerName))
                metadataItems.Add(("العميل", @return.CustomerName));

            metadataItems.Add(("نوع السند", "مرتجع مبيعات"));

            mainSection.Blocks.Add(CreateAlignedInfoTable(metadataItems));
            mainSection.Blocks.Add(CreateSeparator());

            Table table = new Table();
            table.CellSpacing = 0;
            table.Columns.Add(new TableColumn() { Width = new GridLength(185) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(35) });
            table.Columns.Add(new TableColumn() { Width = new GridLength(70) });

            TableRowGroup group = new TableRowGroup();
            TableRow hRow = new TableRow();
            hRow.Cells.Add(new TableCell(new Paragraph(new Run("الصنف")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }));
            hRow.Cells.Add(new TableCell(new Paragraph(new Run("كمية")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
            hRow.Cells.Add(new TableCell(new Paragraph(new Run("إجمالي")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
            group.Rows.Add(hRow);

            TableRow sepRow = new TableRow();
            sepRow.Cells.Add(new TableCell(CreateSeparator()) { ColumnSpan = 3 });
            group.Rows.Add(sepRow);

            foreach (var item in items)
            {
                TableRow row = new TableRow();
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.ProductName)) { TextAlignment = TextAlignment.Center }));
                row.Cells.Add(new TableCell(new Paragraph(new Run(item.Quantity.ToString())) { TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
                row.Cells.Add(new TableCell(new Paragraph(new Run(Formatting.FormatNumber(item.TotalPrice))) { TextAlignment = TextAlignment.Center }) { BorderThickness = new Thickness(1, 0, 0, 0), BorderBrush = Brushes.Black });
                group.Rows.Add(row);
            }

            table.RowGroups.Add(group);
            mainSection.Blocks.Add(table);
            mainSection.Blocks.Add(CreateSeparator());

            var rtItems = new List<(string, string)> { ("إجمالي المرتجع", Formatting.FormatNumber(@return.TotalAmount)) };
            mainSection.Blocks.Add(CreateAlignedInfoTable(rtItems, true));
            mainSection.Blocks.Add(CreateSeparator());

            mainSection.Blocks.Add(new Paragraph(new Run("شكراً لتعاملكم معنا")) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 5, 0, 0) });
            mainSection.Blocks.Add(CreateSeparator());

            doc.Blocks.Add(mainSection);
            return doc;
        }

        private FlowDocument CreateRepairIntakeDocument(RepairOrder order, List<RepairDevice> devices)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Courier New");
            doc.FontSize = 10;
            doc.FlowDirection = FlowDirection.RightToLeft;
            doc.IsOptimalParagraphEnabled = false;
            doc.IsHyphenationEnabled = false;

            Section s = new Section();
            string shopName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");
            string phonesText = GetPhonesText();

            s.Blocks.Add(new Paragraph(new Run(shopName)) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
            if (!string.IsNullOrWhiteSpace(phonesText))
                s.Blocks.Add(new Paragraph(new Run(phonesText)) { FontSize = 9, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 2), FlowDirection = FlowDirection.LeftToRight });
            s.Blocks.Add(new Paragraph(new Run("إيصال استلام جهاز للصيانة")) { FontSize = 11, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 2) });
            s.Blocks.Add(CreateSeparator());

            var info = new List<(string, string)>
            {
                ("رقم الطلب",       order.OrderNumber),
                ("التاريخ",         order.IntakeDate.ToString("yyyy-MM-dd")),
                ("العميل",          order.CustomerName ?? "-"),
                ("الهاتف",          order.CustomerPhone ?? "-"),
                ("الفني",           order.TechnicianName ?? "-"),
                ("التسليم المتوقع", order.ExpectedDelivery?.ToString("yyyy-MM-dd") ?? "-")
            };
            s.Blocks.Add(CreateAlignedInfoTable(info));
            s.Blocks.Add(CreateSeparator());

            s.Blocks.Add(new Paragraph(new Run("الأجهزة المستلمة")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 2) });

            foreach (var d in devices)
            {
                var deviceInfo = new List<(string, string)>
                {
                    ("النوع",      $"{d.DeviceType} {d.Brand} {d.Model}".Trim()),
                    ("السيريال",   string.IsNullOrEmpty(d.SerialNumber) ? "-" : d.SerialNumber),
                    ("الحالة",     string.IsNullOrEmpty(d.Condition) ? "-" : d.Condition),
                    ("المشكلة",    d.ReportedIssue),
                    ("الملحقات",   string.IsNullOrEmpty(d.Accessories) ? "-" : d.Accessories)
                };
                s.Blocks.Add(CreateAlignedInfoTable(deviceInfo));
                s.Blocks.Add(new Paragraph(new Run("· · ·")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 1, 0, 1) });
            }

            s.Blocks.Add(CreateSeparator());
            if (!string.IsNullOrEmpty(order.Notes))
                s.Blocks.Add(new Paragraph(new Run("ملاحظات: " + order.Notes)) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2, 0, 2) });

            s.Blocks.Add(new Paragraph(new Run("توقيع العميل: ___________________")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 2) });
            s.Blocks.Add(CreateSeparator());
            s.Blocks.Add(new Paragraph(new Run("شكراً لثقتكم بنا")) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold });
            s.Blocks.Add(CreateSeparator());

            doc.Blocks.Add(s);
            return doc;
        }

        private FlowDocument CreateRepairInvoiceDocument(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments)
        {
            FlowDocument doc = new FlowDocument();
            doc.FontFamily = new FontFamily("Courier New");
            doc.FontSize = 10;
            doc.FlowDirection = FlowDirection.RightToLeft;
            doc.IsOptimalParagraphEnabled = false;
            doc.IsHyphenationEnabled = false;
            doc.PageWidth = 580;
            doc.PagePadding = new Thickness(20);

            Section s = new Section();
            string shopName = DatabaseManager.Instance.GetSetting("shop_name", "الجوهري");
            string phonesText = GetPhonesText();

            s.Blocks.Add(new Paragraph(new Run(shopName)) { FontSize = 14, FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 4) });
            if (!string.IsNullOrWhiteSpace(phonesText))
                s.Blocks.Add(new Paragraph(new Run(phonesText)) { FontSize = 9, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 2), FlowDirection = FlowDirection.LeftToRight });
            s.Blocks.Add(new Paragraph(new Run("فاتورة صيانة")) { FontSize = 12, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 0, 0, 2) });
            s.Blocks.Add(CreateSeparator());

            var info = new List<(string, string)>
            {
                ("رقم الطلب",  order.OrderNumber),
                ("التاريخ",    order.IntakeDate.ToString("yyyy-MM-dd")),
                ("التسليم",    order.DeliveryDate?.ToString("yyyy-MM-dd") ?? order.IntakeDate.ToString("yyyy-MM-dd")),
                ("العميل",     order.CustomerName ?? "-"),
                ("الهاتف",     order.CustomerPhone ?? "-"),
                ("الفني",      order.TechnicianName ?? "-")
            };
            s.Blocks.Add(CreateAlignedInfoTable(info));
            s.Blocks.Add(CreateSeparator());

            s.Blocks.Add(new Paragraph(new Run("الأجهزة والخدمات")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2, 0, 2) });

            foreach (var d in devices)
            {
                s.Blocks.Add(new Paragraph(new Run($"▪ {d.DisplayName} — {d.ReportedIssue}")) { Margin = new Thickness(0, 1, 0, 0) });
                if (!string.IsNullOrEmpty(d.RepairNotes))
                    s.Blocks.Add(new Paragraph(new Run($"  الإصلاح: {d.RepairNotes}")) { Margin = new Thickness(0, 0, 0, 0), FontSize = 9 });

                var deviceParts = parts.FindAll(p => p.DeviceId == d.Id);
                if (deviceParts.Count > 0)
                {
                    foreach (var p in deviceParts)
                        s.Blocks.Add(new Paragraph(new Run($"  • {p.PartName} x{p.Quantity} = {Formatting.FormatNumber(p.TotalCost)}")) { Margin = new Thickness(0), FontSize = 9 });
                }

                if (d.LaborCost > 0)
                    s.Blocks.Add(new Paragraph(new Run($"  أجر العمل: {Formatting.FormatNumber(d.LaborCost)}")) { Margin = new Thickness(0), FontSize = 9 });
            }

            s.Blocks.Add(CreateSeparator());

            var totals = new List<(string, string)>
            {
                ("الإجمالي",  Formatting.FormatCurrency(order.TotalAmount)),
                ("المدفوع",   Formatting.FormatCurrency(order.PaidAmount)),
                ("المتبقي",   Formatting.FormatCurrency(order.RemainingAmount))
            };
            s.Blocks.Add(CreateAlignedInfoTable(totals, true));
            s.Blocks.Add(CreateSeparator());

            if (payments.Count > 0)
            {
                s.Blocks.Add(new Paragraph(new Run("سجل المدفوعات")) { FontWeight = FontWeights.Bold, TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 2, 0, 2) });
                foreach (var p in payments)
                    s.Blocks.Add(new Paragraph(new Run($"  {p.PaymentDate:yyyy-MM-dd}  |  {p.PaymentMethod}  |  {Formatting.FormatCurrency(p.Amount)}")) { Margin = new Thickness(0), FontSize = 9 });
                s.Blocks.Add(CreateSeparator());
            }

            s.Blocks.Add(new Paragraph(new Run("توقيع العميل: ___________________")) { TextAlignment = TextAlignment.Center, Margin = new Thickness(0, 8, 0, 2) });
            s.Blocks.Add(new Paragraph(new Run("شكراً لثقتكم بنا")) { TextAlignment = TextAlignment.Center, FontWeight = FontWeights.Bold });
            s.Blocks.Add(CreateSeparator());

            doc.Blocks.Add(s);
            return doc;
        }

        private static string GetPhonesText()
        {
            try
            {
                return Formatting.FormatPhonesForPrint(ServiceContainer.GetService<ISettingsService>()?.GetShopPhones());
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
