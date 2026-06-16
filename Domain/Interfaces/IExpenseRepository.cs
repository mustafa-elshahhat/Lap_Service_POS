using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IExpenseRepository
    {
        long Create(Expense expense);
        List<Expense> GetAll();
        List<Expense> GetByDateRange(string startDate, string endDate);
        // Soft delete: marks the expense deleted (is_deleted=1) with audit fields; never hard-deletes.
        void Delete(int id, int deletedBy);
        List<string> GetCategories();
    }
}
