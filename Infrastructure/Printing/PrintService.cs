using System.Collections.Generic;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Application.DTOs;

namespace CarPartsShopWPF.Infrastructure.Printing
{
    public class PrintService : IPrintService
    {
        private readonly ReceiptPrintService _receiptService;
        private readonly ReportPrintService _reportService;

        public PrintService()
        {
            _receiptService = new ReceiptPrintService();
            _reportService = new ReportPrintService();
        }

        public void PrintSaleReceipt(Sale sale, List<SaleItem> items)
        {
            _receiptService.PrintSale(sale, items);
        }

        public void PrintReturnReceipt(Return @return, List<ReturnItem> items)
        {
            _receiptService.PrintReturn(@return, items);
        }

        public void PrintReport(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers)
        {
            _reportService.Print(title, data, columns, headers);
        }

        public void PrintGroupedReport(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders)
        {
            _reportService.PrintGrouped(title, data, itemColumns, itemHeaders);
        }

    }
}
