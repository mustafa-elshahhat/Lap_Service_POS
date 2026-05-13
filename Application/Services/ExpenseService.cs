using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Application.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly IExpenseRepository _expenseRepo;
        private readonly IAuthService _auth;
        private readonly IDbTransactionManager _txManager;

        public ExpenseService(IExpenseRepository expenseRepo, IAuthService auth, IDbTransactionManager txManager)
        {
            _expenseRepo = expenseRepo;
            _auth = auth;
            _txManager = txManager;
        }

        public List<Dictionary<string, object>> GetExpensesByDateRange(string startDate, string endDate)
        {
             var expenses = _expenseRepo.GetByDateRange(startDate, endDate);
             var list = new List<Dictionary<string, object>>();
             foreach (var e in expenses)
             {
                 list.Add(new Dictionary<string, object> {
                     { "id", e.Id },
                     { "description", e.Description },
                     { "amount", e.Amount },
                     { "category", e.Category },
                     { "expense_date", e.ExpenseDate },
                     { "payment_method", e.PaymentMethod },
                     { "user_name", e.UserName }
                 });
             }
             return list;
        }

        public List<Dictionary<string, object>> SearchExpenses(List<Dictionary<string, object>> expenses, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return expenses;

            string q = searchText.ToLower();
            return expenses.FindAll(x => 
                SafeConvert.ToString(x["description"]).ToLower().Contains(q) ||
                SafeConvert.ToString(x["category"]).ToLower().Contains(q)
            );
        }

        public decimal CalculateTotalExpenses(List<Dictionary<string, object>> expenses)
        {
            decimal total = 0;
            foreach (var ex in expenses)
                total += SafeConvert.ToDecimal(ex["amount"]);
            return total;
        }

        public void CreateExpense(string description, decimal amount, string category, string paymentMethod, DateTime date)
        {
            var expense = new Expense
            {
                Description = description,
                Amount = amount,
                Category = category,
                UserId = _auth.GetUserId(),
                PaymentMethod = paymentMethod,
                ExpenseDate = date
            };
            _expenseRepo.Create(expense);
        }

        public void DeleteExpense(int id)
        {
            _expenseRepo.Delete(id);
        }
        public List<string> GetCategories()
        {
            return _expenseRepo.GetCategories();
        }
    }
}
