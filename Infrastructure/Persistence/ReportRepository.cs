using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class ReportRepository : IReportRepository
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;
        private readonly SummaryQueries _summaryQueries;
        private readonly OperationsLogQueries _operationsLogQueries;
        private readonly PaymentBreakdownQueries _paymentBreakdownQueries;

        public ReportRepository()
        {
            _summaryQueries = new SummaryQueries(_db);
            _operationsLogQueries = new OperationsLogQueries(_db);
            _paymentBreakdownQueries = new PaymentBreakdownQueries(_db);
        }

        public Dictionary<string, object> GetDailySummary(string date)
        {
            var range = _summaryQueries.GetDateRange(date);
            return BuildSummaryRange(range.start, range.end);
        }

        public Dictionary<string, object> GetPeriodSummary(string startDate, string endDate)
        {
            var range = _summaryQueries.GetPeriodRange(startDate, endDate);
            return BuildSummaryRange(range.start, range.end);
        }

        private Dictionary<string, object> BuildSummaryRange(string start, string end)
        {
            var summary = _summaryQueries.BuildSummaryRange(start, end);
            var args = new Dictionary<string, object> { { "@start", start }, { "@end", end } };
            _paymentBreakdownQueries.AddPaymentBreakdowns(summary, args);
            return summary;
        }

        public List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate)
        {
            return _operationsLogQueries.GetOperationsReport(startDate, endDate);
        }

        public List<Dictionary<string, object>> GetFinancialOperations(string startDate, string endDate)
        {
            return _operationsLogQueries.GetFinancialOperations(startDate, endDate);
        }
    }
}
