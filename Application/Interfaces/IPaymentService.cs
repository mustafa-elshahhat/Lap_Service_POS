using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface IPaymentService
    {
        Dictionary<string, object> AddPayment(int saleId, decimal amount, int receivedBy, string notes = null, string paymentMethod = "نقدي");
        bool ValidatePaymentAmount(int saleId, decimal paymentAmount);
        Dictionary<string, decimal> GetPaymentBreakdown(int saleId);
        List<Dictionary<string, object>> GetCustomerPaymentHistory(int customerId);
        void AddPaymentHistory(int customerId, int saleId, string type, decimal amount, decimal before, decimal after, int receivedBy, string notes);
    }
}
