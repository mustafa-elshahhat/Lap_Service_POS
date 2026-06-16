using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly IActivityLog _activityLog;

        public ExpenseService(IExpenseRepository expenseRepo, IAuthService auth, IDbTransactionManager txManager, IActivityLog activityLog = null)
        {
            _expenseRepo = expenseRepo;
            _auth = auth;
            _txManager = txManager;
            _activityLog = activityLog;
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

        // Categories that must NOT be recorded as a generic expense — they are cash-out flows owned by
        // dedicated screens (supplier payments, salaries). Recording them here would double-count cash.
        private static readonly string[] ProtectedCategories = { "مرتبات", "رواتب", "سداد مورد", "مدفوعات موردين" };

        public static bool IsProtectedCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category)) return false;
            string c = category.Trim();
            return ProtectedCategories.Any(p => string.Equals(p, c, StringComparison.OrdinalIgnoreCase));
        }

        public void CreateExpense(string description, decimal amount, string category, string paymentMethod, DateTime date)
        {
            if (IsProtectedCategory(category))
                throw new InvalidOperationException(
                    "هذه الفئة محجوزة. سجّل مدفوعات الموردين من شاشة الموردين والرواتب من شاشة الموظفين حتى لا يتكرر احتساب النقدية.");

            int userId = _auth.GetUserId();
            var expense = new Expense
            {
                Description = description,
                Amount = amount,
                Category = category,
                UserId = userId,
                PaymentMethod = paymentMethod,
                ExpenseDate = date
            };

            if (_txManager == null)
            {
                _expenseRepo.Create(expense);
                return;
            }

            _txManager.BeginTransaction();
            try
            {
                long id = _expenseRepo.Create(expense);
                _activityLog.LogActivity(userId, "create_expense", "expenses", (int)id,
                    $"{category}: {description} - {amount} ({paymentMethod})");
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public void DeleteExpense(int id)
        {
            int userId = _auth.GetUserId();

            if (_txManager == null)
            {
                _expenseRepo.Delete(id, userId);
                return;
            }

            _txManager.BeginTransaction();
            try
            {
                _expenseRepo.Delete(id, userId);
                _activityLog.LogActivity(userId, "delete_expense", "expenses", id,
                    "Soft-deleted expense.");
                _txManager.CommitTransaction();
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }
        public List<string> GetCategories()
        {
            return _expenseRepo.GetCategories();
        }
    }
}
