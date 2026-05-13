using System.Collections.Generic;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface IReportService
    {
        Dictionary<string, object> GetDailySummary(string targetDate = null);
        Dictionary<string, object> GetMonthlySummary(int year, int month);
        Dictionary<string, object> GetProfitSummary(string startDate, string endDate);
        List<Dictionary<string, object>> GetOperationsReport(string startDate, string endDate);
    }
}
