using System;
using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IExpenseService
    {
        List<Dictionary<string, object>> GetExpensesByDateRange(string startDate, string endDate);
        List<Dictionary<string, object>> SearchExpenses(List<Dictionary<string, object>> expenses, string searchText);
        decimal CalculateTotalExpenses(List<Dictionary<string, object>> expenses);
        void CreateExpense(string description, decimal amount, string category, string paymentMethod, DateTime date);
        void DeleteExpense(int id);
        List<string> GetCategories();
    }
}
