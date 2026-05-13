using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IReturnService
    {
        Dictionary<string, object> CreateReturn(int saleId, List<ReturnItem> items, int userId, string reason = null, string refundMethod = "نقدي");
        Dictionary<string, object> GetReturnById(int returnId);
        List<Dictionary<string, object>> GetReturnItems(int returnId);
        Dictionary<int, int> GetReturnedQuantities(int saleId);
        List<Return> GetReturns(string query = null);
        List<Return> GetReturnsReport(string startDate, string endDate);
    }
}
