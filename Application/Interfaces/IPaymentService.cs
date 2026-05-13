using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IPaymentService
    {
        Dictionary<string, decimal> GetPaymentBreakdown(int saleId);
    }
}
