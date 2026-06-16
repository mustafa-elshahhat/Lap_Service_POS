using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly DatabaseManager _db;

        public EmployeeRepository()
        {
            _db = DatabaseManager.Instance;
        }

        private Employee MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Employee
            {
                Id = SafeConvert.ToInt(row["id"]),
                FullName = SafeConvert.ToString(row["full_name"]),
                Phone = SafeConvert.ToString(row["phone"]),
                JobTitle = SafeConvert.ToString(row["job_title"]),
                BaseSalary = SafeConvert.ToDecimal(row["base_salary"]),
                Notes = SafeConvert.ToString(row["notes"]),
                IsActive = SafeConvert.ToBool(row["is_active"]),
                CreatedAt = SafeConvert.ToDateTime(row["created_at"]) ?? DateTime.MinValue,
                UpdatedAt = SafeConvert.ToDateTime(row["updated_at"]) ?? DateTime.MinValue
            };
        }

        public List<Employee> GetAll(bool includeInactive = false)
        {
            string sql = "SELECT id, full_name, phone, job_title, base_salary, notes, is_active, created_at, updated_at FROM employees";
            if (!includeInactive) sql += " WHERE is_active = 1";
            sql += " ORDER BY full_name";

            var rows = _db.FetchAll(sql);
            var list = new List<Employee>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<Employee> GetActive()
        {
            return GetAll(false);
        }

        public Employee GetById(int id)
        {
            var row = _db.FetchOne("SELECT id, full_name, phone, job_title, base_salary, notes, is_active, created_at, updated_at FROM employees WHERE id = @id",
                new Dictionary<string, object> { { "@id", id } });
            return MapToEntity(row);
        }

        public long Create(Employee employee)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO employees (full_name, phone, job_title, base_salary, notes, is_active, created_at, updated_at)
                VALUES (@fullName, @phone, @jobTitle, @baseSalary, @notes, 1, datetime('now'), datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@fullName", employee.FullName },
                    { "@phone", employee.Phone },
                    { "@jobTitle", employee.JobTitle },
                    { "@baseSalary", employee.BaseSalary },
                    { "@notes", employee.Notes }
                });
        }

        public void Update(Employee employee)
        {
            _db.Execute(@"
                UPDATE employees
                SET full_name = @fullName,
                    phone = @phone,
                    job_title = @jobTitle,
                    base_salary = @baseSalary,
                    notes = @notes,
                    updated_at = datetime('now')
                WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@fullName", employee.FullName },
                    { "@phone", employee.Phone },
                    { "@jobTitle", employee.JobTitle },
                    { "@baseSalary", employee.BaseSalary },
                    { "@notes", employee.Notes },
                    { "@id", employee.Id }
                });
        }

        public void SetActive(int id, bool isActive)
        {
            _db.Execute("UPDATE employees SET is_active = @active, updated_at = datetime('now') WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@active", isActive ? 1 : 0 },
                    { "@id", id }
                });
        }

        public long AddSalaryTransaction(int employeeId, string transactionType, decimal amount, string paymentMethod, DateTime transactionDate, string notes, int? createdBy)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO employee_salary_transactions
                    (employee_id, transaction_type, amount, payment_method, transaction_date, notes, created_by, created_at)
                VALUES
                    (@employeeId, @transactionType, @amount, @paymentMethod, @transactionDate, @notes, @createdBy, datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@employeeId", employeeId },
                    { "@transactionType", transactionType },
                    { "@amount", amount },
                    { "@paymentMethod", paymentMethod },
                    { "@transactionDate", transactionDate.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@notes", notes },
                    { "@createdBy", createdBy }
                });
        }

        public Dictionary<string, object> GetSalaryTransactionById(long id)
        {
            return _db.FetchOne(@"
                SELECT t.*, e.full_name as employee_name, u.full_name as created_by_name
                FROM employee_salary_transactions t
                JOIN employees e ON t.employee_id = e.id
                LEFT JOIN users u ON t.created_by = u.id
                WHERE t.id = @id",
                new Dictionary<string, object> { { "@id", id } });
        }

        public List<Dictionary<string, object>> GetSalaryTransactions(int employeeId)
        {
            return _db.FetchAll(@"
                SELECT t.*, e.full_name as employee_name, u.full_name as created_by_name
                FROM employee_salary_transactions t
                JOIN employees e ON t.employee_id = e.id
                LEFT JOIN users u ON t.created_by = u.id
                WHERE t.employee_id = @employeeId
                ORDER BY t.transaction_date DESC, t.id DESC",
                new Dictionary<string, object> { { "@employeeId", employeeId } });
        }
    }
}
