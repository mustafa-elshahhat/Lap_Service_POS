using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class ReportRepository : IReportRepository
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;

        // GetDailySummary(date) and GetPeriodSummary(start,end) both delegate to the single
        // BuildSummaryRange below so the financial formulas live in exactly one place (previously the
        // two methods were near-duplicate and each computed net_cash_flow/net_profit twice).
        public Dictionary<string, object> GetDailySummary(string date)
        {
            var range = GetDateRange(date);
            return BuildSummaryRange(range.start, range.end);
        }

        public Dictionary<string, object> GetPeriodSummary(string startDate, string endDate)
        {
            var range = GetPeriodRange(startDate, endDate);
            return BuildSummaryRange(range.start, range.end);
        }

        // Single source of truth for the day/period financial summary.
        // Canonical formulas (locked here and by tests):
        //   net_cash_flow = (cash_received + maintenance_total)
        //                   - cash_refunds - total_expenses - total_supplier_payments - total_salary_payments
        //   net_profit    = (SUM(sales.profit) + maintenance_profit)
        //                   - lost_profit - total_expenses - net_salary_expense
        // Maintenance cash and maintenance profit are both recognized by repair payment_date.
        // Profit is payment-proportional against labor + parts margin; delivery_date is not used.
        private Dictionary<string, object> BuildSummaryRange(string start, string end)
        {
            var summary = new Dictionary<string, object>();
            var args = new Dictionary<string, object> { { "@start", start }, { "@end", end } };

            // NOTE (de-scoped): credit_sales (= SUM(remaining_amount)) and payments_received (old-debt
            // collection) are structurally always 0 — credit sales / receivables are unsupported. The
            // keys are retained only so downstream SafeConvert lookups never crash; never surface them.
            var salesQuery = _db.FetchOne(@"
                SELECT
                    COALESCE(SUM(total_amount), 0) as gross_sales,
                    COALESCE(SUM(remaining_amount), 0) as credit_sales,
                    COALESCE(SUM(paid_amount), 0) as cash_from_new_sales,
                    COUNT(*) as invoice_count,
                    COALESCE(SUM(profit), 0) as total_profit_new_sales
                FROM sales
                WHERE sale_date >= @start AND sale_date < @end", args);

            summary["gross_sales"] = SafeConvert.ToDecimal(salesQuery["gross_sales"]);
            summary["credit_sales"] = SafeConvert.ToDecimal(salesQuery["credit_sales"]);
            summary["cash_from_new_sales"] = SafeConvert.ToDecimal(salesQuery["cash_from_new_sales"]);
            summary["invoice_count"] = SafeConvert.ToInt(salesQuery["invoice_count"]);
            decimal grossProfitFromSales = SafeConvert.ToDecimal(salesQuery["total_profit_new_sales"]);

            var totalReceivedQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total_received
                FROM sale_payments
                WHERE payment_date >= @start AND payment_date < @end", args);
            decimal totalCashReceived = SafeConvert.ToDecimal(totalReceivedQuery["total_received"]);
            summary["cash_received"] = totalCashReceived;

            var oldPaymentsQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(sp.amount), 0) as old_payments
                FROM sale_payments sp
                JOIN sales s ON sp.sale_id = s.id
                WHERE sp.payment_date >= @start AND sp.payment_date < @end
                AND s.sale_date < @start", args);
            summary["payments_received"] = SafeConvert.ToDecimal(oldPaymentsQuery["old_payments"]);

            var returnsQuery = _db.FetchOne(@"
                SELECT
                    COALESCE(SUM(total_amount), 0) as returns_value,
                    COALESCE(SUM(cash_refund), 0) as cash_refunds,
                    COALESCE(SUM(debt_deduction), 0) as debt_cancelled
                FROM returns
                WHERE return_date >= @start AND return_date < @end", args);
            summary["returns_value"] = SafeConvert.ToDecimal(returnsQuery["returns_value"]);
            summary["cash_refunds"] = SafeConvert.ToDecimal(returnsQuery["cash_refunds"]);
            // debt_cancelled is legacy (credit unsupported) and is always 0; not surfaced as a KPI.
            summary["debt_cancelled"] = SafeConvert.ToDecimal(returnsQuery["debt_cancelled"]);

            var expensesQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total FROM expenses
                WHERE expense_date >= @start AND expense_date < @end
                  AND COALESCE(is_deleted, 0) = 0", args);
            summary["total_expenses"] = SafeConvert.ToDecimal(expensesQuery["total"]);

            var supplierPaymentsQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total
                FROM supplier_transactions
                WHERE transaction_type = 'payment'
                AND transaction_date >= @start AND transaction_date < @end", args);
            summary["total_supplier_payments"] = SafeConvert.ToDecimal(supplierPaymentsQuery["total"]);

            AddEmployeeSalarySummary(summary, args);
            decimal totalSalaryPayments = SafeConvert.ToDecimal(summary["total_salary_payments"]);
            decimal netSalaryExpense = SafeConvert.ToDecimal(summary["net_salary_expense"]);

            var lostProfitQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(
                    (ri.total_price - (ri.quantity * si.unit_purchase_price))
                ), 0) as lost
                FROM return_items ri
                JOIN sale_items si ON ri.sale_item_id = si.id
                JOIN returns r ON ri.return_id = r.id
                WHERE r.return_date >= @start AND r.return_date < @end", args);
            decimal lostProfit = SafeConvert.ToDecimal(lostProfitQuery["lost"]);

            // Maintenance cash (recognized by payment_date).
            var maintenanceCashQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as maintenance_total
                FROM repair_payments
                WHERE payment_date >= @start AND payment_date < @end", args);
            decimal maintenanceCashReceived = SafeConvert.ToDecimal(maintenanceCashQuery["maintenance_total"]);
            summary["maintenance_total"] = maintenanceCashReceived;

            // Maintenance profit (recognized by payment_date): each payment recognizes the same
            // proportion of order profit as the payment covers of order revenue. This avoids double
            // counting across multiple payments and caps legacy overpayments at total order profit.
            decimal maintenanceProfit = CalculateMaintenanceProfitForPayments(start, end);
            summary["maintenance_profit"] = maintenanceProfit;

            // Profit (computed once, with maintenance).
            summary["gross_profit"] = grossProfitFromSales + maintenanceProfit;
            summary["lost_profit"]  = lostProfit;
            summary["net_profit"]   = grossProfitFromSales + maintenanceProfit
                                      - lostProfit
                                      - SafeConvert.ToDecimal(summary["total_expenses"])
                                      - netSalaryExpense;

            // Cash flow (computed once, with maintenance).
            summary["net_cash_flow"] = totalCashReceived
                                      + maintenanceCashReceived
                                      - SafeConvert.ToDecimal(summary["cash_refunds"])
                                      - SafeConvert.ToDecimal(summary["total_expenses"])
                                      - SafeConvert.ToDecimal(summary["total_supplier_payments"])
                                      - totalSalaryPayments;

            AddPaymentBreakdowns(summary, args);

            return summary;
        }

        private decimal CalculateMaintenanceProfitForPayments(string start, string end)
        {
            var args = new Dictionary<string, object> { { "@start", start }, { "@end", end } };
            var rows = _db.FetchAll(@"
                SELECT
                    rp.id,
                    rp.order_id,
                    rp.amount,
                    CASE WHEN rp.payment_date >= @start AND rp.payment_date < @end THEN 1 ELSE 0 END as in_period,
                    COALESCE((SELECT SUM(rd.labor_cost)
                              FROM repair_devices rd
                              WHERE rd.order_id = rp.order_id), 0) as labor_revenue,
                    COALESCE((SELECT SUM(part.total_cost)
                              FROM repair_parts part
                              WHERE part.order_id = rp.order_id), 0) as parts_revenue,
                    COALESCE((SELECT SUM(part.total_cost - (part.purchase_cost * part.quantity))
                              FROM repair_parts part
                              WHERE part.order_id = rp.order_id), 0) as parts_profit
                FROM repair_payments rp
                WHERE rp.payment_date < @end
                  AND EXISTS (
                      SELECT 1 FROM repair_payments period_payment
                      WHERE period_payment.order_id = rp.order_id
                        AND period_payment.payment_date >= @start
                        AND period_payment.payment_date < @end
                  )
                ORDER BY rp.order_id, rp.payment_date, rp.id", args);

            decimal periodProfit = 0m;
            long currentOrderId = -1;
            decimal paidBefore = 0m;
            decimal totalRevenue = 0m;
            decimal totalProfit = 0m;

            foreach (var row in rows)
            {
                long orderId = SafeConvert.ToLong(row["order_id"]);
                if (orderId != currentOrderId)
                {
                    currentOrderId = orderId;
                    paidBefore = 0m;

                    decimal laborRevenue = SafeConvert.ToDecimal(row["labor_revenue"]);
                    decimal partsRevenue = SafeConvert.ToDecimal(row["parts_revenue"]);
                    decimal partsProfit = SafeConvert.ToDecimal(row["parts_profit"]);
                    totalRevenue = laborRevenue + partsRevenue;
                    totalProfit = laborRevenue + partsProfit;
                }

                decimal paymentAmount = SafeConvert.ToDecimal(row["amount"]);
                decimal paymentProfit = 0m;

                if (totalRevenue > 0m && totalProfit > 0m && paymentAmount > 0m)
                {
                    decimal paidAfter = paidBefore + paymentAmount;
                    decimal recognizedRevenueBefore = Math.Min(paidBefore, totalRevenue);
                    decimal recognizedRevenueAfter = Math.Min(paidAfter, totalRevenue);
                    decimal recognizedPaymentRevenue = Math.Max(0m, recognizedRevenueAfter - recognizedRevenueBefore);
                    paymentProfit = (recognizedPaymentRevenue / totalRevenue) * totalProfit;
                }

                if (SafeConvert.ToInt(row["in_period"]) == 1)
                    periodProfit += paymentProfit;

                paidBefore += paymentAmount;
            }

            return periodProfit;
        }

        public List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate)
        {
            var range = GetPeriodRange(startDate, endDate);
            var args = new Dictionary<string, object> { { "@start", range.start }, { "@end", range.end } };

            return _db.FetchAll(@"
                SELECT 'بيع' as OperationName, invoice_number as Reference,
                       COALESCE(c.name, 'عميل نقدي') as Details, total_amount as Amount,
                       remaining_amount as Remaining,
                       'نقدي' as SaleType,
                       payment_method as PaymentMethod,
                       sale_date as Date, u.full_name as UserName
                FROM sales s
                LEFT JOIN customers c ON s.customer_id = c.id
                LEFT JOIN users u ON s.user_id = u.id
                WHERE sale_date >= @start AND sale_date < @end

                UNION ALL

                SELECT 'استرجاع' as OperationName, return_number as Reference,
                       COALESCE(c.name, 'عميل نقدي') as Details, total_amount as Amount,
                       0 as Remaining, 'N/A' as SaleType, payment_method as PaymentMethod,
                       return_date as Date, u.full_name as UserName
                FROM returns r
                LEFT JOIN customers c ON r.customer_id = c.id
                LEFT JOIN users u ON r.user_id = u.id
                WHERE return_date >= @start AND return_date < @end

                UNION ALL

                SELECT 'مصروفات' as OperationName, 'N/A' as Reference,
                       description as Details, amount as Amount,
                       0 as Remaining, 'N/A' as SaleType, payment_method as PaymentMethod,
                       expense_date as Date, u.full_name as UserName
                FROM expenses e
                LEFT JOIN users u ON e.user_id = u.id
                WHERE expense_date >= @start AND expense_date < @end
                  AND COALESCE(e.is_deleted, 0) = 0

                UNION ALL

                SELECT 'سداد مورد' as OperationName, 'N/A' as Reference,
                       sup.name as Details, amount as Amount,
                       0 as Remaining, 'N/A' as SaleType, payment_method as PaymentMethod,
                       transaction_date as Date, u.full_name as UserName
                FROM supplier_transactions st
                JOIN suppliers sup ON st.supplier_id = sup.id
                LEFT JOIN users u ON st.created_by = u.id
                WHERE st.transaction_type = 'payment' AND transaction_date >= @start AND transaction_date < @end

                UNION ALL

                SELECT CASE WHEN est.transaction_type = 'salary' THEN 'مرتب موظف' ELSE 'خصم موظف' END as OperationName,
                       'N/A' as Reference,
                       e.full_name as Details,
                       est.amount as Amount,
                       0 as Remaining,
                       'N/A' as SaleType,
                       CASE WHEN est.transaction_type = 'salary' THEN COALESCE(est.payment_method, 'نقدي') ELSE 'خصم' END as PaymentMethod,
                       est.transaction_date as Date,
                       u.full_name as UserName
                FROM employee_salary_transactions est
                JOIN employees e ON est.employee_id = e.id
                LEFT JOIN users u ON est.created_by = u.id
                WHERE est.transaction_date >= @start AND est.transaction_date < @end

                UNION ALL

                SELECT 'صيانة' as OperationName, ro.order_number as Reference,
                       COALESCE(ro.customer_name, 'عميل') as Details, ro.paid_amount as Amount,
                       ro.remaining_amount as Remaining,
                       'N/A' as SaleType,
                       CASE WHEN (SELECT COUNT(*) FROM repair_payments rp WHERE rp.order_id = ro.id) > 1
                            THEN 'متعدد'
                            ELSE COALESCE(
                                   (SELECT payment_method FROM repair_payments
                                    WHERE order_id = ro.id ORDER BY payment_date DESC LIMIT 1),
                                   'نقدي')
                       END as PaymentMethod,
                       ro.delivery_date as Date, u.full_name as UserName
                FROM repair_orders ro
                LEFT JOIN users u ON ro.user_id = u.id
                WHERE ro.order_status = 'delivered'
                  AND ro.delivery_date >= @start AND ro.delivery_date < @end

                ORDER BY Date DESC",
                args);
        }

        private void AddPaymentBreakdowns(Dictionary<string, object> summary, Dictionary<string, object> args)
        {
            var inflowRows = _db.FetchAll(@"
                SELECT payment_method, SUM(amount) as total FROM (
                    SELECT payment_method, amount FROM sale_payments
                    WHERE payment_date >= @start AND payment_date < @end
                    UNION ALL
                    SELECT payment_method, amount FROM repair_payments
                    WHERE payment_date >= @start AND payment_date < @end
                ) GROUP BY payment_method", args);

            var inflows = new Dictionary<string, decimal>();
            foreach (var r in inflowRows)
                inflows[SafeConvert.ToString(r["payment_method"])] = SafeConvert.ToDecimal(r["total"]);

            // Outflows by method: expenses (excluding soft-deleted) + supplier payments + salary
            // payments + cash refunds. Refunds are money-out and MUST appear here so the per-method
            // outflow cards reconcile with net_cash_flow's outflow side.
            var outflowRows = _db.FetchAll(@"
                SELECT payment_method, SUM(amount) as total FROM (
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
                ) GROUP BY payment_method", args);

            var outflows = new Dictionary<string, decimal>();
            foreach (var r in outflowRows)
                outflows[SafeConvert.ToString(r["payment_method"])] = SafeConvert.ToDecimal(r["total"]);

            summary["payment_inflows"]  = inflows;
            summary["payment_outflows"] = outflows;
            summary["payment_details"]  = inflows;
        }

        private void AddEmployeeSalarySummary(Dictionary<string, object> summary, Dictionary<string, object> args)
        {
            var salaryQuery = _db.FetchOne(@"
                SELECT
                    COALESCE(SUM(CASE WHEN transaction_type = 'salary' THEN amount ELSE 0 END), 0) as total_salary_payments,
                    COALESCE(SUM(CASE WHEN transaction_type = 'deduction' THEN amount ELSE 0 END), 0) as total_employee_deductions
                FROM employee_salary_transactions
                WHERE transaction_date >= @start AND transaction_date < @end", args);

            decimal salaryPayments = SafeConvert.ToDecimal(salaryQuery["total_salary_payments"]);
            decimal deductions = SafeConvert.ToDecimal(salaryQuery["total_employee_deductions"]);

            summary["total_salary_payments"] = salaryPayments;
            summary["total_employee_deductions"] = deductions;
            summary["net_salary_expense"] = salaryPayments - deductions;
        }

        private (string start, string end) GetDateRange(string date)
        {
            if (string.IsNullOrWhiteSpace(date)) date = DateTime.Now.ToString("yyyy-MM-dd");
            DateTime dt = DateTime.Parse(date);
            return (dt.ToString("yyyy-MM-dd 00:00:00"), dt.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
        }

        private (string start, string end) GetPeriodRange(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start)) start = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(end)) end = start;

            DateTime dtStart = DateTime.Parse(start);
            DateTime dtEnd = DateTime.Parse(end);
            return (dtStart.ToString("yyyy-MM-dd 00:00:00"), dtEnd.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
        }
    }
}
