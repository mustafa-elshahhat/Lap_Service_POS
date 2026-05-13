using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Application.DTOs;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface IPrintService
    {
        void PrintSaleReceipt(Sale sale, List<SaleItem> items);
        void PrintReturnReceipt(Return @return, List<ReturnItem> items);
        void PrintReport(string title, IEnumerable<Dictionary<string, object>> data, string[] columns, string[] headers);
        void PrintGroupedReport(string title, IEnumerable<GroupedReportItem> data, string[] itemColumns, string[] itemHeaders);
    }
}
