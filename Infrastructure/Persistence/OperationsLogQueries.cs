using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class OperationsLogQueries
    {
        private readonly DatabaseManager _db;

        public OperationsLogQueries(DatabaseManager db)
        {
            _db = db;
        }

        private const string OperationsReportSql = @"
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

            ORDER BY Date DESC";

        private const string FinancialOperationsSql = @"
            -- بيع (sale payment received)
            SELECT sp.payment_date as Date, 'بيع' as OperationType,
                   s.invoice_number as Reference,
                   COALESCE(c.name, 'عميل نقدي') as Details,
                   CASE WHEN sp.payment_method IS NULL OR sp.payment_method = '' THEN 'غير محدد'
                        ELSE sp.payment_method END as PaymentMethod,
                   sp.amount as MoneyIn, 0 as MoneyOut, 0 as Deduction,
                   sp.amount as NetEffect, u.full_name as UserName
            FROM sale_payments sp
            JOIN sales s ON sp.sale_id = s.id
            LEFT JOIN customers c ON s.customer_id = c.id
            LEFT JOIN users u ON s.user_id = u.id
            WHERE sp.payment_date >= @start AND sp.payment_date < @end

            UNION ALL

            -- تحصيل صيانة (repair/maintenance payment received)
            SELECT rp.payment_date as Date, 'تحصيل صيانة' as OperationType,
                   ro.order_number as Reference,
                   COALESCE(ro.customer_name, 'عميل') as Details,
                   CASE WHEN rp.payment_method IS NULL OR rp.payment_method = '' THEN 'غير محدد'
                        ELSE rp.payment_method END as PaymentMethod,
                   rp.amount as MoneyIn, 0 as MoneyOut, 0 as Deduction,
                   rp.amount as NetEffect, u.full_name as UserName
            FROM repair_payments rp
            JOIN repair_orders ro ON rp.order_id = ro.id
            LEFT JOIN users u ON rp.user_id = u.id
            WHERE rp.payment_date >= @start AND rp.payment_date < @end

            UNION ALL

            -- مصروف (expense, non-deleted only)
            SELECT e.expense_date as Date, 'مصروف' as OperationType,
                   '' as Reference, e.description as Details,
                   CASE WHEN e.payment_method IS NULL OR e.payment_method = '' THEN 'غير محدد'
                        ELSE e.payment_method END as PaymentMethod,
                   0 as MoneyIn, e.amount as MoneyOut, 0 as Deduction,
                   -e.amount as NetEffect, u.full_name as UserName
            FROM expenses e
            LEFT JOIN users u ON e.user_id = u.id
            WHERE e.expense_date >= @start AND e.expense_date < @end
              AND COALESCE(e.is_deleted, 0) = 0

            UNION ALL

            -- دفع مورد (supplier payment only; purchases are never cash out)
            SELECT st.transaction_date as Date, 'دفع مورد' as OperationType,
                   COALESCE(st.reference_number, '') as Reference, sup.name as Details,
                   CASE WHEN st.payment_method IS NULL OR st.payment_method = '' THEN 'غير محدد'
                        ELSE st.payment_method END as PaymentMethod,
                   0 as MoneyIn, st.amount as MoneyOut, 0 as Deduction,
                   -st.amount as NetEffect, u.full_name as UserName
            FROM supplier_transactions st
            JOIN suppliers sup ON st.supplier_id = sup.id
            LEFT JOIN users u ON st.created_by = u.id
            WHERE st.transaction_type = 'payment'
              AND st.transaction_date >= @start AND st.transaction_date < @end

            UNION ALL

            -- راتب (salary payment)
            SELECT est.transaction_date as Date, 'راتب' as OperationType,
                   '' as Reference, emp.full_name as Details,
                   CASE WHEN est.payment_method IS NULL OR est.payment_method = '' THEN 'غير محدد'
                        ELSE est.payment_method END as PaymentMethod,
                   0 as MoneyIn, est.amount as MoneyOut, 0 as Deduction,
                   -est.amount as NetEffect, u.full_name as UserName
            FROM employee_salary_transactions est
            JOIN employees emp ON est.employee_id = emp.id
            LEFT JOIN users u ON est.created_by = u.id
            WHERE est.transaction_type = 'salary'
              AND est.transaction_date >= @start AND est.transaction_date < @end

            UNION ALL

            -- خصم موظف (employee deduction: cost reducer, NOT cash in)
            SELECT est.transaction_date as Date, 'خصم موظف' as OperationType,
                   '' as Reference, emp.full_name as Details,
                   'خصم' as PaymentMethod,
                   0 as MoneyIn, 0 as MoneyOut, est.amount as Deduction,
                   est.amount as NetEffect, u.full_name as UserName
            FROM employee_salary_transactions est
            JOIN employees emp ON est.employee_id = emp.id
            LEFT JOIN users u ON est.created_by = u.id
            WHERE est.transaction_type = 'deduction'
              AND est.transaction_date >= @start AND est.transaction_date < @end

            UNION ALL

            -- استرداد نقدي (cash refund on a return)
            SELECT r.return_date as Date, 'استرداد نقدي' as OperationType,
                   r.return_number as Reference,
                   COALESCE(c.name, 'عميل نقدي') as Details,
                   CASE WHEN r.payment_method IS NULL OR r.payment_method = '' THEN 'غير محدد'
                        ELSE r.payment_method END as PaymentMethod,
                   0 as MoneyIn, r.cash_refund as MoneyOut, 0 as Deduction,
                   -r.cash_refund as NetEffect, u.full_name as UserName
            FROM returns r
            LEFT JOIN customers c ON r.customer_id = c.id
            LEFT JOIN users u ON r.user_id = u.id
            WHERE r.return_date >= @start AND r.return_date < @end
              AND r.cash_refund > 0

            ORDER BY Date";

        public List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate)
        {
            var range = GetPeriodRange(startDate, endDate);
            var args = new Dictionary<string, object> { { "@start", range.start }, { "@end", range.end } };
            return _db.FetchAll(OperationsReportSql, args);
        }

        public List<Dictionary<string, object>> GetFinancialOperations(string startDate, string endDate)
        {
            var range = GetPeriodRange(startDate, endDate);
            var args = new Dictionary<string, object> { { "@start", range.start }, { "@end", range.end } };
            return _db.FetchAll(FinancialOperationsSql, args);
        }

        private static (string start, string end) GetPeriodRange(string start, string end)
        {
            if (string.IsNullOrWhiteSpace(start)) start = DateTime.Now.ToString("yyyy-MM-dd");
            if (string.IsNullOrWhiteSpace(end)) end = start;

            var dtStart = DateTime.Parse(start);
            var dtEnd = DateTime.Parse(end);
            return (dtStart.ToString("yyyy-MM-dd 00:00:00"), dtEnd.AddDays(1).ToString("yyyy-MM-dd 00:00:00"));
        }
    }
}
