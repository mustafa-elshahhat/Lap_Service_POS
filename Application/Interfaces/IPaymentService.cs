using System.Collections.Generic;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface IPaymentService
    {
        Dictionary<string, decimal> GetPaymentBreakdown(int saleId);
    }
}
