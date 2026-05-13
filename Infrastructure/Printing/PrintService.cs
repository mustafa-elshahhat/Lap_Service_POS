using System.Collections.Generic;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Application.DTOs;

namespace CarPartsShopWPF.Infrastructure.Printing
{
    public class PrintService : IPrintService
    {
        private readonly InvoicePrintService _invoiceService;
        private readonly ReportPrintService  _reportService;

        public PrintService()
        {
            _invoiceService = new InvoicePrintService();
            _reportService  = new ReportPrintService();
        }

        public void PrintSaleReceipt(Sale sale, List<SaleItem> items)
        {
            _invoiceService.PrintSale(sale, items);
        }

        public void PrintReturnReceipt(Return @return, List<ReturnItem> items)
        {
            _invoiceService.PrintReturn(@return, items);
        }

        public void PrintReport(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers)
        {
            _reportService.Print(title, data, columns, headers);
        }

        public void PrintGroupedReport(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders)
        {
            _reportService.PrintGrouped(title, data, itemColumns, itemHeaders);
        }

        public void PrintRepairIntake(RepairOrder order, List<RepairDevice> devices)
        {
            _invoiceService.PrintRepairIntake(order, devices);
        }

        public void PrintRepairInvoice(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments)
        {
            _invoiceService.PrintRepairInvoice(order, devices, parts, payments);
        }
    }
}
