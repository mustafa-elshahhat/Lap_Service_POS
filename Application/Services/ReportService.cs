using System;
using System.Collections.Generic;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Application.Services
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

        public Dictionary<string, object> GetProfitSummary(string startDate, string endDate)
        {
            var summary = GetPeriodSummary(startDate, endDate);
            return new Dictionary<string, object>
            {
                { "revenue", summary["gross_sales"] },
                { "gross_profit", summary["gross_profit"] },
                { "returns_value", summary["returns_value"] },
                { "total_expenses", summary["total_expenses"] },
                { "net_profit", summary["net_profit"] }
            };
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
                { "returns_value", SafeConvert.ToDecimal(data["returns_value"]) },
                { "cash_refunds", SafeConvert.ToDecimal(data["cash_refunds"]) },
                { "debt_cancelled", SafeConvert.ToDecimal(data["debt_cancelled"]) },
                { "total_supplier_payments", SafeConvert.ToDecimal(data["total_supplier_payments"]) },
                { "net_cash_flow", SafeConvert.ToDecimal(data["net_cash_flow"]) },
                { "gross_profit", SafeConvert.ToDecimal(data["gross_profit"]) },
                { "lost_profit", SafeConvert.ToDecimal(data["lost_profit"]) },
                { "net_profit", SafeConvert.ToDecimal(data["net_profit"]) },
                { "payment_details", data["payment_details"] }
            };
        }

        public List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate)
        {
            return _reportRepo.GetOperationsReport(startDate, endDate);
        }
    }
}
