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
        private readonly ReceiptDocumentBuilder _builder = new ReceiptDocumentBuilder();

        public void PrintSale(Sale sale, List<SaleItem> items)
        {
            var doc = _builder.CreateSaleDocument(sale, items);
            PrintDocument(doc, "SaleInvoice_" + sale.InvoiceNumber);
        }

        public void PrintReturn(Return @return, List<ReturnItem> items)
        {
            var doc = _builder.CreateReturnDocument(@return, items);
            PrintDocument(doc, "ReturnReceipt_" + @return.ReturnNumber);
        }

        public void PrintRepairIntake(RepairOrder order, List<RepairDevice> devices)
        {
            var doc = _builder.CreateRepairIntakeDocument(order, devices);
            PrintDocument(doc, "RepairIntake_" + order.OrderNumber);
        }

        public void PrintRepairInvoice(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments)
        {
            var doc = _builder.CreateRepairInvoiceDocument(order, devices, parts, payments);
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
                        catch // best-effort: page-size estimation fallback
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
                    } catch {} // best-effort: printer may not support custom PageMediaSize

                    printDialog.PrintDocument(paginator, jobName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("خطأ أثناء الطباعة: " + ex.Message, "خطأ طباعة", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}
