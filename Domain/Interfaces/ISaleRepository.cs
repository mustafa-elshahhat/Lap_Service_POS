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

        // SUPERSEDED by IReportRepository.GetDailySummary / GetPeriodSummary which include
        // maintenance profit, expenses, returns, supplier payments, and salary data. This method
        // only reads the sales table and gives an incomplete financial picture.
        [Obsolete("Use IReportRepository.GetDailySummary / GetPeriodSummary for the full financial summary.")]
        Dictionary<string, object> GetDailySummary(DateTime date);
        // SUPERSEDED by IReportRepository.GetPeriodSummary which includes maintenance profit,
        // expenses, returns, supplier payments, and salary data.
        [Obsolete("Use IReportRepository.GetPeriodSummary for the full period financial summary.")]
        Dictionary<string, object> GetMonthlySummary(DateTime startDate, DateTime endDate);
        List<Sale> GetTodaySales();

        void UpdateSaleFinancials(long saleId, decimal total, decimal paid, decimal remaining, decimal profit);

        string GenerateInvoiceNumber();
        void LogActivity(int userId, string action, string table, int recordId, string details);
    }
}
