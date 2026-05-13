using System;
using System.Collections.Generic;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Persistence
{
    public class ReportRepository : IReportRepository
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;

        public Dictionary<string, object> GetDailySummary(string date)
        {
            var summary = new Dictionary<string, object>();
            var range = GetDateRange(date);
            var args = new Dictionary<string, object> { { "@start", range.start }, { "@end", range.end } };

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
            summary["debt_cancelled"] = SafeConvert.ToDecimal(returnsQuery["debt_cancelled"]);

            var expensesQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total FROM expenses
                WHERE expense_date >= @start AND expense_date < @end", args);
            summary["total_expenses"] = SafeConvert.ToDecimal(expensesQuery["total"]);

            var supplierPaymentsQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total
                FROM supplier_transactions
                WHERE transaction_type = 'payment'
                AND transaction_date >= @start AND transaction_date < @end", args);
            summary["total_supplier_payments"] = SafeConvert.ToDecimal(supplierPaymentsQuery["total"]);

            var lostProfitQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(
                    (ri.total_price - (ri.quantity * si.unit_purchase_price))
                ), 0) as lost
                FROM return_items ri
                JOIN sale_items si ON ri.sale_item_id = si.id
                JOIN returns r ON ri.return_id = r.id
                WHERE r.return_date >= @start AND r.return_date < @end", args);

            decimal lostProfit = SafeConvert.ToDecimal(lostProfitQuery["lost"]);

            summary["gross_profit"] = grossProfitFromSales;
            summary["lost_profit"] = lostProfit;
            summary["net_profit"] = grossProfitFromSales - lostProfit - SafeConvert.ToDecimal(summary["total_expenses"]);

            summary["net_cash_flow"] = totalCashReceived
                                     - SafeConvert.ToDecimal(summary["cash_refunds"])
                                     - SafeConvert.ToDecimal(summary["total_expenses"])
                                     - SafeConvert.ToDecimal(summary["total_supplier_payments"]);

            var maintenanceCashQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as maintenance_total
                FROM repair_payments
                WHERE payment_date >= @start AND payment_date < @end", args);
            decimal maintenanceCashReceived = SafeConvert.ToDecimal(maintenanceCashQuery["maintenance_total"]);
            summary["maintenance_total"] = maintenanceCashReceived;

            var maintenancePartsProfit = _db.FetchOne(@"
                SELECT COALESCE(SUM(rp.total_cost - (rp.purchase_cost * rp.quantity)), 0) as parts_profit
                FROM repair_parts rp
                JOIN repair_orders ro ON rp.order_id = ro.id
                WHERE ro.order_status = 'delivered'
                  AND ro.delivery_date >= @start AND ro.delivery_date < @end", args);

            var maintenanceLaborProfit = _db.FetchOne(@"
                SELECT COALESCE(SUM(rd.labor_cost), 0) as labor_profit
                FROM repair_devices rd
                JOIN repair_orders ro ON rd.order_id = ro.id
                WHERE ro.order_status = 'delivered'
                  AND ro.delivery_date >= @start AND ro.delivery_date < @end", args);

            decimal maintenanceProfit = SafeConvert.ToDecimal(maintenancePartsProfit["parts_profit"])
                                      + SafeConvert.ToDecimal(maintenanceLaborProfit["labor_profit"]);
            summary["maintenance_profit"] = maintenanceProfit;

            summary["gross_profit"] = grossProfitFromSales + maintenanceProfit;
            summary["lost_profit"]  = lostProfit;
            summary["net_profit"]   = (decimal)summary["gross_profit"] - lostProfit - SafeConvert.ToDecimal(summary["total_expenses"]);

            summary["net_cash_flow"] = totalCashReceived
                                     + maintenanceCashReceived
                                     - SafeConvert.ToDecimal(summary["cash_refunds"])
                                     - SafeConvert.ToDecimal(summary["total_expenses"])
                                     - SafeConvert.ToDecimal(summary["total_supplier_payments"]);

            AddPaymentBreakdowns(summary, args);

            return summary;
        }


        public Dictionary<string, object> GetPeriodSummary(string startDate, string endDate)
        {
            var summary = new Dictionary<string, object>();
            var range = GetPeriodRange(startDate, endDate);
            var args = new Dictionary<string, object> { { "@start", range.start }, { "@end", range.end } };

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
            summary["cash_received"] = SafeConvert.ToDecimal(totalReceivedQuery["total_received"]);

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
            summary["debt_cancelled"] = SafeConvert.ToDecimal(returnsQuery["debt_cancelled"]);

            var expensesQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total FROM expenses
                WHERE expense_date >= @start AND expense_date < @end", args);
            summary["total_expenses"] = SafeConvert.ToDecimal(expensesQuery["total"]);

            var supplierPaymentsQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as total 
                FROM supplier_transactions
                WHERE transaction_type = 'payment' 
                AND transaction_date >= @start AND transaction_date < @end", args);
            summary["total_supplier_payments"] = SafeConvert.ToDecimal(supplierPaymentsQuery["total"]);

            var lostProfitQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(
                    (ri.total_price - (ri.quantity * si.unit_purchase_price))
                ), 0) as lost
                FROM return_items ri
                JOIN sale_items si ON ri.sale_item_id = si.id
                JOIN returns r ON ri.return_id = r.id
                WHERE r.return_date >= @start AND r.return_date < @end", args);

            decimal lostProfit = SafeConvert.ToDecimal(lostProfitQuery["lost"]);

            summary["gross_profit"] = grossProfitFromSales;
            summary["lost_profit"] = lostProfit;
            summary["net_profit"] = grossProfitFromSales - lostProfit - SafeConvert.ToDecimal(summary["total_expenses"]);

            summary["net_cash_flow"] = SafeConvert.ToDecimal(summary["cash_received"]) 
                                     - SafeConvert.ToDecimal(summary["cash_refunds"]) 
                                     - SafeConvert.ToDecimal(summary["total_expenses"]) 
                                     - SafeConvert.ToDecimal(summary["total_supplier_payments"]);

            var maintenanceCashPeriodQuery = _db.FetchOne(@"
                SELECT COALESCE(SUM(amount), 0) as maintenance_total
                FROM repair_payments
                WHERE payment_date >= @start AND payment_date < @end", args);
            decimal maintenanceCashPeriod = SafeConvert.ToDecimal(maintenanceCashPeriodQuery["maintenance_total"]);
            summary["maintenance_total"] = maintenanceCashPeriod;

            var maintenancePartsProfitPeriod = _db.FetchOne(@"
                SELECT COALESCE(SUM(rp.total_cost - (rp.purchase_cost * rp.quantity)), 0) as parts_profit
                FROM repair_parts rp
                JOIN repair_orders ro ON rp.order_id = ro.id
                WHERE ro.order_status = 'delivered'
                  AND ro.delivery_date >= @start AND ro.delivery_date < @end", args);

            var maintenanceLaborProfitPeriod = _db.FetchOne(@"
                SELECT COALESCE(SUM(rd.labor_cost), 0) as labor_profit
                FROM repair_devices rd
                JOIN repair_orders ro ON rd.order_id = ro.id
                WHERE ro.order_status = 'delivered'
                  AND ro.delivery_date >= @start AND ro.delivery_date < @end", args);

            decimal maintenanceProfitPeriod = SafeConvert.ToDecimal(maintenancePartsProfitPeriod["parts_profit"])
                                            + SafeConvert.ToDecimal(maintenanceLaborProfitPeriod["labor_profit"]);
            summary["maintenance_profit"] = maintenanceProfitPeriod;

            summary["gross_profit"] = grossProfitFromSales + maintenanceProfitPeriod;
            summary["lost_profit"]  = lostProfit;
            summary["net_profit"]   = (decimal)summary["gross_profit"] - lostProfit - SafeConvert.ToDecimal(summary["total_expenses"]);

            summary["net_cash_flow"] = SafeConvert.ToDecimal(summary["cash_received"])
                                     + maintenanceCashPeriod
                                     - SafeConvert.ToDecimal(summary["cash_refunds"])
                                     - SafeConvert.ToDecimal(summary["total_expenses"])
                                     - SafeConvert.ToDecimal(summary["total_supplier_payments"]);

            AddPaymentBreakdowns(summary, args);

            return summary;
        }

        public List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate)
        {
            var range = GetPeriodRange(startDate, endDate);
            var args = new Dictionary<string, object> { { "@start", range.start }, { "@end", range.end } };

            return _db.FetchAll(@"
                SELECT 'بيع' as OperationName, invoice_number as Reference,
                       COALESCE(c.name, 'عميل نقدي') as Details, total_amount as Amount,
                       remaining_amount as Remaining,
                       CASE WHEN sale_type = 'cash' THEN 'نقدي' ELSE 'آجل' END as SaleType,
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

            var outflowRows = _db.FetchAll(@"
                SELECT payment_method, SUM(amount) as total FROM (
                    SELECT payment_method, amount FROM expenses
                    WHERE expense_date >= @start AND expense_date < @end
                    UNION ALL
                    SELECT payment_method, amount FROM supplier_transactions
                    WHERE transaction_type = 'payment'
                    AND transaction_date >= @start AND transaction_date < @end
                ) GROUP BY payment_method", args);

            var outflows = new Dictionary<string, decimal>();
            foreach (var r in outflowRows)
                outflows[SafeConvert.ToString(r["payment_method"])] = SafeConvert.ToDecimal(r["total"]);

            summary["payment_inflows"]  = inflows;
            summary["payment_outflows"] = outflows;
            summary["payment_details"]  = inflows;
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
