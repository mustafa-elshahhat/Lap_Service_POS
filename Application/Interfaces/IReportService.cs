using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IReportService
    {
        Dictionary<string, object> GetDailySummary(string targetDate = null);
        Dictionary<string, object> GetMonthlySummary(int year, int month);
        List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate);
    }
}
