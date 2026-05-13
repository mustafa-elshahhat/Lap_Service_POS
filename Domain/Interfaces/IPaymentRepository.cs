using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        void AddPaymentHistory(Payment payment);
        List<Payment> GetPaymentHistory(int customerId);
        Dictionary<string, decimal> GetPaymentBreakdown(long saleId);
        List<Payment> GetPaymentsByDateRange(string startDate, string endDate);
    }
}
