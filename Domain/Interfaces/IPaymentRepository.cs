using System.Collections.Generic;

namespace CarPartsShopWPF.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Dictionary<string, decimal> GetPaymentBreakdown(long saleId);
    }
}
