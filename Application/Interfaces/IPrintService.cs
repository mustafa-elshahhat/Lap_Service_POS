using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Application.DTOs;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IPrintService
    {
        void PrintSaleReceipt(Sale sale, List<SaleItem> items);
        void PrintReturnReceipt(Return @return, List<ReturnItem> items);
        void PrintInventory(List<Product> products, Dictionary<string, object> totals);
        void PrintSupplierStatement(string supplierName, IEnumerable<Dictionary<string, object>> transactions, Dictionary<int, List<SupplierPurchaseItem>> transactionItems);
        void PrintReport(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers);
        void PrintGroupedReport(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders);
        void PrintRepairIntake(RepairOrder order, List<RepairDevice> devices);
        void PrintRepairInvoice(RepairOrder order, List<RepairDevice> devices, List<RepairPart> parts, List<RepairPayment> payments);
    }
}
