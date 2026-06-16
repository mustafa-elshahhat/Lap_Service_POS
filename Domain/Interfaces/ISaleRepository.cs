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
        List<Sale> GetSalesReport(string startDate, string endDate);

        void AddSaleItem(long saleId, SaleItem item);
        List<SaleItem> GetItems(long saleId);
        void UpdateSaleItemFinancials(int itemId, decimal paid, decimal remaining);

        void UpdatePaymentStatus(long saleId, decimal paid, decimal remaining);
        void AddSalePayment(long saleId, string method, decimal amount, string notes);
        List<Dictionary<string, object>> GetSalePayments(long saleId);
        List<Sale> GetTodaySales();

        string GenerateInvoiceNumber();
        void LogActivity(int userId, string action, string table, int recordId, string details);
    }
}
