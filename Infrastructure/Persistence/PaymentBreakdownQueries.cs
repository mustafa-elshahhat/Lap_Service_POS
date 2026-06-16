using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class PaymentBreakdownQueries
    {
        private readonly DatabaseManager _db;

        public PaymentBreakdownQueries(DatabaseManager db)
        {
            _db = db;
        }

        private const string InflowSql = @"
            SELECT CASE WHEN payment_method IS NULL OR payment_method = '' THEN 'غير محدد' ELSE payment_method END as payment_method,
                   SUM(amount) as total FROM (
                SELECT payment_method, amount FROM sale_payments
                WHERE payment_date >= @start AND payment_date < @end
                UNION ALL
                SELECT payment_method, amount FROM repair_payments
                WHERE payment_date >= @start AND payment_date < @end
            ) GROUP BY CASE WHEN payment_method IS NULL OR payment_method = '' THEN 'غير محدد' ELSE payment_method END";

        private const string OutflowSql = @"
            SELECT CASE WHEN payment_method IS NULL OR payment_method = '' THEN 'غير محدد' ELSE payment_method END as payment_method,
                   SUM(amount) as total FROM (
                SELECT payment_method, amount FROM expenses
                WHERE expense_date >= @start AND expense_date < @end
                AND COALESCE(is_deleted, 0) = 0
                UNION ALL
                SELECT payment_method, amount FROM supplier_transactions
                WHERE transaction_type = 'payment'
                AND transaction_date >= @start AND transaction_date < @end
                UNION ALL
                SELECT payment_method, amount FROM employee_salary_transactions
                WHERE transaction_type = 'salary'
                AND transaction_date >= @start AND transaction_date < @end
                UNION ALL
                SELECT payment_method, cash_refund AS amount FROM returns
                WHERE return_date >= @start AND return_date < @end
                AND cash_refund > 0
            ) GROUP BY CASE WHEN payment_method IS NULL OR payment_method = '' THEN 'غير محدد' ELSE payment_method END";

        public void AddPaymentBreakdowns(Dictionary<string, object> summary, Dictionary<string, object> args)
        {
            var inflowRows = _db.FetchAll(InflowSql, args);

            var inflows = new Dictionary<string, decimal>();
            foreach (var r in inflowRows)
                inflows[SafeConvert.ToString(r["payment_method"])] = SafeConvert.ToDecimal(r["total"]);

            var outflowRows = _db.FetchAll(OutflowSql, args);

            var outflows = new Dictionary<string, decimal>();
            foreach (var r in outflowRows)
                outflows[SafeConvert.ToString(r["payment_method"])] = SafeConvert.ToDecimal(r["total"]);

            summary["payment_inflows"] = inflows;
            summary["payment_outflows"] = outflows;
        }
    }
}
