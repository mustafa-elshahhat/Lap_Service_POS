using System;
using System.Collections.Generic;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Persistence
{
    public class ExpenseRepository : IExpenseRepository
    {
        private readonly DatabaseManager _db;

        public ExpenseRepository()
        {
             _db = DatabaseManager.Instance;
        }

        private Expense MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Expense
            {
                Id = SafeConvert.ToInt(row["id"]),
                Description = SafeConvert.ToString(row["description"]),
                Amount = SafeConvert.ToDecimal(row["amount"]),
                Category = SafeConvert.ToString(row["category"]),
                UserId = SafeConvert.ToInt(row["user_id"]),
                PaymentMethod = SafeConvert.ToString(row["payment_method"]),
                ExpenseDate = SafeConvert.ToDateTime(row["expense_date"]) ?? DateTime.MinValue,
                UserName = SafeConvert.ToString(row["user_name"])
            };
        }

        public long Create(Expense expense)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO expenses (description, amount, category, user_id, expense_date, payment_method)
                VALUES (@desc, @amount, @cat, @userId, @date, @method)",
                new Dictionary<string, object>
                {
                    { "@desc", expense.Description },
                    { "@amount", expense.Amount },
                    { "@cat", expense.Category },
                    { "@userId", expense.UserId },
                    { "@date", expense.ExpenseDate.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@method", expense.PaymentMethod }
                });
        }

        public List<Expense> GetAll()
        {
            var rows = _db.FetchAll(@"
                SELECT e.*, u.full_name as user_name
                FROM expenses e
                LEFT JOIN users u ON e.user_id = u.id
                ORDER BY e.expense_date DESC
                LIMIT 100");
            
            var list = new List<Expense>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<Expense> GetByDateRange(string startDate, string endDate)
        {
            string start = startDate + " 00:00:00";
            string end = DateTime.Parse(endDate).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            var rows = _db.FetchAll(@"
                SELECT e.*, u.full_name as user_name
                FROM expenses e
                LEFT JOIN users u ON e.user_id = u.id
                WHERE e.expense_date >= @start AND e.expense_date < @end
                ORDER BY e.expense_date DESC",
                new Dictionary<string, object> { { "@start", start }, { "@end", end } });

            var list = new List<Expense>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public void Delete(int id)
        {
            _db.Execute("DELETE FROM expenses WHERE id = @id", new Dictionary<string, object> { { "@id", id } });
        }

        public List<string> GetCategories()
        {
            var rows = _db.FetchAll("SELECT DISTINCT category FROM expenses WHERE category IS NOT NULL");
            var list = new List<string>();
            foreach (var row in rows)
            {
                string c = SafeConvert.ToString(row["category"]);
                if (!string.IsNullOrEmpty(c)) list.Add(c.Trim());
            }
            return list;
        }
    }
}
