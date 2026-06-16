using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IReturnRepository
    {
        long CreateReturn(string returnNumber, int saleId, int? customerId, int userId, decimal total, decimal cashRefund, decimal debtDeduction, string reason, string method);
        void AddReturnItem(long returnId, int saleItemId, int productId, string code, string name, int quantity, decimal unitPrice, decimal total);
        List<Return> GetReturns(string query);
        Dictionary<string, object> GetReturnById(int id);
        List<Dictionary<string, object>> GetReturnItems(int returnId);
        Dictionary<int, int> GetReturnedQuantities(int saleId);
        List<Return> GetReturnsReport(string startDate, string endDate);
        string GenerateReturnNumber();
    }
}
