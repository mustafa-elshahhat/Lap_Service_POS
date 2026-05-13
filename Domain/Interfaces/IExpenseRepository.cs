using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Domain.Interfaces
{
    public interface IExpenseRepository
    {
        long Create(Expense expense);
        List<Expense> GetAll();
        List<Expense> GetByDateRange(string startDate, string endDate);
        void Delete(int id);
        List<string> GetCategories();
    }
}
