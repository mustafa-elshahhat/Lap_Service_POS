using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class SummaryQueries
    {
        private readonly DatabaseManager _db;

        public SummaryQueries(DatabaseManager db)
        {
            _db = db;
        }

        private const string SalesAggregationSql = @"
            SELECT
                COALESCE(SUM(total_amount), 0) as gross_sales,
                COALESCE(SUM(remaining_amount), 0) as credit_sales,
                COALESCE(SUM(paid_amount), 0) as cash_from_new_sales,
                COUNT(*) as invoice_count,
                COALESCE(SUM(profit), 0) as total_profit_new_sales
            FROM sales
            WHERE sale_date >= @start AND sale_date < @end";

        private const string TotalCashReceivedSql = @"
            SELECT COALESCE(SUM(amount), 0) as total_received
            FROM sale_payments
            WHERE payment_date >= @start AND payment_date < @end";

        private const string OldPaymentsSql = @"
            SELECT COALESCE(SUM(sp.amount), 0) as old_payments
            FROM sale_payments sp
            JOIN sales s ON sp.sale_id = s.id
            WHERE sp.payment_date >= @start AND sp.payment_date < @end
            AND s.sale_date < @start";

        private const string ReturnsSql = @"
            SELECT
                COALESCE(SUM(total_amount), 0) as returns_value,
                COALESCE(SUM(cash_refund), 0) as cash_refunds,
                COALESCE(SUM(debt_deduction), 0) as debt_cancelled
            FROM returns
            WHERE return_date >= @start AND return_date < @end";

        private const string ExpensesSql = @"
            SELECT COALESCE(SUM(amount), 0) as total FROM expenses
            WHERE expense_date >= @start AND expense_date < @end
              AND COALESCE(is_deleted, 0) = 0";

        private const string SupplierPaymentsSql = @"
            SELECT COALESCE(SUM(amount), 0) as total
            FROM supplier_transactions
            WHERE transaction_type = 'payment'
            AND transaction_date >= @start AND transaction_date < @end";

        private const string LostProfitSql = @"
            SELECT COALESCE(SUM(
                (ri.total_price - (ri.quantity * si.unit_purchase_price))
            ), 0) as lost
            FROM return_items ri
            JOIN sale_items si ON ri.sale_item_id = si.id
            JOIN returns r ON ri.return_id = r.id
            WHERE r.return_date >= @start AND r.return_date < @end";

        private const string MaintenanceCashSql = @"
            SELECT COALESCE(SUM(amount), 0) as maintenance_total
            FROM repair_payments
            WHERE payment_date >= @start AND payment_date < @end";

        private const string MaintenanceProfitSql = @"
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
            ORDER BY rp.order_id, rp.payment_date, rp.id";

        private const string EmployeeSalarySummarySql = @"
            SELECT
                COALESCE(SUM(CASE WHEN transaction_type = 'salary' THEN amount ELSE 0 END), 0) as total_salary_payments,
                COALESCE(SUM(CASE WHEN transaction_type = 'deduction' THEN amount ELSE 0 END), 0) as total_employee_deductions
            FROM employee_salary_transactions
            WHERE transaction_date >= @start AND transaction_date < @end";

        public Dictionary<string, object> BuildSummaryRange(string start, string end)
        {
            var summary = new Dictionary<string, object>();
            var args = new Dictionary<string, object> { { "@start", start }, { "@end", end } };

            var salesQuery = _db.FetchOne(SalesAggregationSql, args);
            summary["gross_sales"] = SafeConvert.ToDecimal(salesQuery["gross_sales"]);
            summary["credit_sales"] = SafeConvert.ToDecimal(salesQuery["credit_sales"]);
            summary["cash_from_new_sales"] = SafeConvert.ToDecimal(salesQuery["cash_from_new_sales"]);
            summary["invoice_count"] = SafeConvert.ToInt(salesQuery["invoice_count"]);
            decimal grossProfitFromSales = SafeConvert.ToDecimal(salesQuery["total_profit_new_sales"]);

            var totalReceivedQuery = _db.FetchOne(TotalCashReceivedSql, args);
            decimal totalCashReceived = SafeConvert.ToDecimal(totalReceivedQuery["total_received"]);
            summary["cash_received"] = totalCashReceived;

            var oldPaymentsQuery = _db.FetchOne(OldPaymentsSql, args);
            summary["payments_received"] = SafeConvert.ToDecimal(oldPaymentsQuery["old_payments"]);

            var returnsQuery = _db.FetchOne(ReturnsSql, args);
            summary["returns_value"] = SafeConvert.ToDecimal(returnsQuery["returns_value"]);
            summary["cash_refunds"] = SafeConvert.ToDecimal(returnsQuery["cash_refunds"]);
            summary["debt_cancelled"] = SafeConvert.ToDecimal(returnsQuery["debt_cancelled"]);

            var expensesQuery = _db.FetchOne(ExpensesSql, args);
            summary["total_expenses"] = SafeConvert.ToDecimal(expensesQuery["total"]);

            var supplierPaymentsQuery = _db.FetchOne(SupplierPaymentsSql, args);
            summary["total_supplier_payments"] = SafeConvert.ToDecimal(supplierPaymentsQuery["total"]);

            AddEmployeeSalarySummary(summary, args);
            decimal totalSalaryPayments = SafeConvert.ToDecimal(summary["total_salary_payments"]);
            decimal netSalaryExpense = SafeConvert.ToDecimal(summary["net_salary_expense"]);

            var lostProfitQuery = _db.FetchOne(LostProfitSql, args);
            decimal lostProfit = SafeConvert.ToDecimal(lostProfitQuery["lost"]);

            var maintenanceCashQuery = _db.FetchOne(MaintenanceCashSql, args);
            decimal maintenanceCashReceived = SafeConvert.ToDecimal(maintenanceCashQuery["maintenance_total"]);
            summary["maintenance_total"] = maintenanceCashReceived;

            decimal maintenanceProfit = CalculateMaintenanceProfitForPayments(start, end);
            summary["maintenance_profit"] = maintenanceProfit;

            summary["gross_profit"] = grossProfitFromSales + maintenanceProfit;
            summary["lost_profit"] = lostProfit;
            summary["net_profit"] = grossProfitFromSales + maintenanceProfit
                                    - lostProfit
                                    - SafeConvert.ToDecimal(summary["total_expenses"])
                                    - netSalaryExpense;

            return summary;
        }

        public decimal CalculateMaintenanceProfitForPayments(string start, string end)
        {
            var args = new Dictionary<string, object> { { "@start", start }, { "@end", end } };
            var rows = _db.FetchAll(MaintenanceProfitSql, args);

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

        public void AddEmployeeSalarySummary(Dictionary<string, object> summary, Dictionary<string, object> args)
        {
            var salaryQuery = _db.FetchOne(EmployeeSalarySummarySql, args);

            decimal salaryPayments = SafeConvert.ToDecimal(salaryQuery["total_salary_payments"]);
            decimal deductions = SafeConvert.ToDecimal(salaryQuery["total_employee_deductions"]);

            summary["total_salary_payments"] = salaryPayments;
            summary["total_employee_deductions"] = deductions;
            summary["net_salary_expense"] = salaryPayments - deductions;
        }

        public (string start, string end) GetDateRange(string date)
        {
            if (string.IsNullOrWhiteSpace(date)) date = DateTime.Now.ToString("yyyy-MM-dd");
            DateTime dt = DateTime.Parse(date);
            return (dt.ToString("yyyy-MM-dd 00:00:00"), dt.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
        }

        public (string start, string end) GetPeriodRange(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start)) start = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(end)) end = start;

            DateTime dtStart = DateTime.Parse(start);
            DateTime dtEnd = DateTime.Parse(end);
            return (dtStart.ToString("yyyy-MM-dd 00:00:00"), dtEnd.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
        }
    }
}
