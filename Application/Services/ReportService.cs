using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Application.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepository _reportRepo;

        public ReportService(IReportRepository reportRepo)
        {
            _reportRepo = reportRepo;
        }

        public Dictionary<string, object> GetDailySummary(string targetDate = null)
        {
            if (string.IsNullOrEmpty(targetDate)) targetDate = DateTime.Now.ToString("yyyy-MM-dd");
            var data = _reportRepo.GetDailySummary(targetDate);

            return data;
        }

        public Dictionary<string, object> GetMonthlySummary(int year, int month)
        {
            string startDate = $"{year}-{month:D2}-01";
            string endDate = new DateTime(year, month, DateTime.DaysInMonth(year, month)).ToString("yyyy-MM-dd");
            return GetPeriodSummary(startDate, endDate);
        }

        private Dictionary<string, object> GetPeriodSummary(string startDate, string endDate)
        {
            var data = _reportRepo.GetPeriodSummary(startDate, endDate);

            return new Dictionary<string, object>
            {
                { "gross_sales", SafeConvert.ToDecimal(data["gross_sales"]) },
                { "cash_received", SafeConvert.ToDecimal(data["cash_received"]) },
                { "credit_sales", SafeConvert.ToDecimal(data["credit_sales"]) },
                { "payments_received", SafeConvert.ToDecimal(data["payments_received"]) },
                { "invoice_count", data["invoice_count"] },
                { "total_expenses", SafeConvert.ToDecimal(data["total_expenses"]) },
                { "total_salary_payments", data.ContainsKey("total_salary_payments") ? SafeConvert.ToDecimal(data["total_salary_payments"]) : 0m },
                { "total_employee_deductions", data.ContainsKey("total_employee_deductions") ? SafeConvert.ToDecimal(data["total_employee_deductions"]) : 0m },
                { "net_salary_expense", data.ContainsKey("net_salary_expense") ? SafeConvert.ToDecimal(data["net_salary_expense"]) : 0m },
                { "returns_value", SafeConvert.ToDecimal(data["returns_value"]) },
                { "cash_refunds", SafeConvert.ToDecimal(data["cash_refunds"]) },
                { "debt_cancelled", SafeConvert.ToDecimal(data["debt_cancelled"]) },
                { "total_supplier_payments", SafeConvert.ToDecimal(data["total_supplier_payments"]) },
                { "net_cash_flow", SafeConvert.ToDecimal(data["net_cash_flow"]) },
                { "gross_profit", SafeConvert.ToDecimal(data["gross_profit"]) },
                { "lost_profit", SafeConvert.ToDecimal(data["lost_profit"]) },
                { "net_profit", SafeConvert.ToDecimal(data["net_profit"]) },
                { "maintenance_total",  SafeConvert.ToDecimal(data["maintenance_total"]) },
                { "maintenance_profit", data.ContainsKey("maintenance_profit") ? SafeConvert.ToDecimal(data["maintenance_profit"]) : 0m },
                { "payment_inflows",  data.ContainsKey("payment_inflows")  ? data["payment_inflows"]  : new Dictionary<string, decimal>() },
                { "payment_outflows", data.ContainsKey("payment_outflows") ? data["payment_outflows"] : new Dictionary<string, decimal>() },
            };
        }

        public List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate)
        {
            return _reportRepo.GetOperationsReport(startDate, endDate);
        }

        // Detailed financial operations log (audit-safe): every money movement recognized by its
        // own transaction/payment date. Powers the daily/monthly "العمليات المالية" pages.
        public List<Dictionary<string, object>> GetFinancialOperations(string startDate, string endDate)
        {
            return _reportRepo.GetFinancialOperations(startDate, endDate);
        }
    }
}
