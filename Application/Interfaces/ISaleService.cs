using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Application.DTOs;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface ISaleService
    {
        SaleOperationResult CreateSale(string saleType, int userId, List<SaleItem> items,
            int? customerId = null, decimal discountAmount = 0, decimal markupAmount = 0,
            decimal paidAmount = 0, string notes = null,
            string paymentMethod = null,
            List<Dictionary<string, object>> paymentMethodsList = null);

        SaleOperationResult CreateCashSale(List<SaleItem> items,
            string customerName = null, string customerPhone = null,
            decimal discountAmount = 0, decimal markupAmount = 0,
            string notes = null, string paymentMethod = "كاش");

        Sale GetSaleById(int id);
        List<SaleItem> GetSaleItems(int saleId);
        List<Sale> GetSales(string query = null);
        List<Sale> GetSalesByCustomer(int customerId);
        List<Sale> GetSalesReport(string startDate, string endDate);
        Sale GetByInvoiceNumber(string invoiceNumber);
        Dictionary<int, int> GetReturnedQuantities(int saleId);
        Dictionary<string, decimal> GetSalePaymentsBreakdown(int saleId);
        Dictionary<string, object> CreateReturn(int saleId, List<ReturnItem> items, int userId, string reason = null, string refundMethod = "نقدي");
        void SaveCustomer(Customer customer);
    }
}
