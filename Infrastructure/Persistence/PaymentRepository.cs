using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly DatabaseManager _db;

        public PaymentRepository()
        {
             _db = DatabaseManager.Instance;
        }

        public Dictionary<string, decimal> GetPaymentBreakdown(long saleId)
        {
            var payments = new Dictionary<string, decimal>();

            var initial = _db.FetchAll(@"
                SELECT payment_method, SUM(amount) as total
                FROM sale_payments WHERE sale_id = @saleId GROUP BY payment_method",
                new Dictionary<string, object> { { "@saleId", saleId } });

            foreach (var item in initial)
            {
                string method = SafeConvert.ToString(item["payment_method"]) ?? "نقدي";
                decimal amount = SafeConvert.ToDecimal(item["total"]);
                if (payments.ContainsKey(method)) payments[method] += amount;
                else payments[method] = amount;
            }
            
            return payments;
        }
    }
}
