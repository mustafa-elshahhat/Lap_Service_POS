using System.Collections.Generic;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IReportRepository
    {
        Dictionary<string, object> GetDailySummary(string date);
        Dictionary<string, object> GetPeriodSummary(string startDate, string endDate);
        List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate);
        List<Dictionary<string, object>> GetFinancialOperations(string startDate, string endDate);
    }
}
