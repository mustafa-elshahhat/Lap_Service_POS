using System.Collections.Generic;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IPaymentRepository
    {
        Dictionary<string, decimal> GetPaymentBreakdown(long saleId);
    }
}
