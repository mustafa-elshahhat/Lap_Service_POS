using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface ISaleRepository
    {
        long Create(Sale sale);
        void Update(Sale sale);
        Sale GetById(long id);
        Sale GetByInvoiceNumber(string invoiceNumber);

        List<Sale> GetAll(int limit = 100);
        List<Sale> Search(string query, int limit = 50);
        List<Sale> GetByCustomerId(int customerId);
        // LEGACY / DE-SCOPED: credit sales and customer receivables are unsupported. No caller.
        [Obsolete("Credit sales / receivables are not supported. All sales are fully paid; no unpaid sales exist.")]
        List<Sale> GetUnpaidByCustomer(int customerId);
        List<Sale> GetSalesReport(string startDate, string endDate);

        void AddSaleItem(long saleId, SaleItem item);
        List<SaleItem> GetItems(long saleId);
        void UpdateItemPayment(int itemId, decimal newPaidAmount);
        void UpdateSaleItemFinancials(int itemId, decimal paid, decimal remaining);
        // LEGACY / DE-SCOPED: dead receivable-era helper, no caller. ReturnService uses UpdateSaleItemFinancials.
        [Obsolete("Unused legacy helper from the credit/receivable era. Not called anywhere.")]
        void UpdateSaleItemFinancialsAfterReturn(int itemId, decimal newTotalPrice, decimal newProfit);

        void updatePaymentStatus(long saleId, decimal paid, decimal remaining);
        void AddSalePayment(long saleId, string method, decimal amount, string notes);
        List<Dictionary<string, object>> GetSalePayments(long saleId);

        Dictionary<string, object> GetDailySummary(DateTime date);
        Dictionary<string, object> GetMonthlySummary(DateTime startDate, DateTime endDate);
        List<Sale> GetTodaySales();

        long CreateReturn(string returnNumber, int saleId, int? customerId, int userId, decimal total, decimal cashRefund, decimal debtDeduction, string reason, string method);
        void AddReturnItem(long returnId, int saleItemId, int productId, string code, string name, int quantity, decimal unitPrice, decimal total);
        List<Return> GetReturns(string query);
        Dictionary<string, object> GetReturnById(int id);
        List<Dictionary<string, object>> GetReturnItems(int returnId);
        Dictionary<int, int> GetReturnedQuantities(int saleId);
        List<Return> GetReturnsReport(string startDate, string endDate);

        void UpdateSaleFinancials(long saleId, decimal total, decimal paid, decimal remaining, decimal profit);

        string GenerateInvoiceNumber();
        string GenerateReturnNumber();
        void LogActivity(int userId, string action, string table, int recordId, string details);
    }
}
