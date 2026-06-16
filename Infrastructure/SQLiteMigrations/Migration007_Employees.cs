using System;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.SQLiteMigrations
{
    public static class Migration007_Employees
    {
        public static void Execute()
        {
            var db = DatabaseManager.Instance;

            try
            {
                db.Execute(@"CREATE TABLE IF NOT EXISTS employees (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    full_name TEXT NOT NULL,
                    phone TEXT,
                    job_title TEXT,
                    base_salary REAL NOT NULL DEFAULT 0,
                    notes TEXT,
                    is_active INTEGER NOT NULL DEFAULT 1,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )");

                db.Execute(@"CREATE TABLE IF NOT EXISTS employee_salary_transactions (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    employee_id INTEGER NOT NULL,
                    transaction_type TEXT NOT NULL CHECK(transaction_type IN ('salary', 'deduction')),
                    amount REAL NOT NULL,
                    payment_method TEXT,
                    transaction_date TIMESTAMP NOT NULL,
                    notes TEXT,
                    created_by INTEGER,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY(employee_id) REFERENCES employees(id),
                    FOREIGN KEY(created_by) REFERENCES users(id)
                )");

                db.EnsureColumnExists("users", "employee_id", "INTEGER NULL REFERENCES employees(id)");

                db.Execute("CREATE INDEX IF NOT EXISTS idx_employees_name ON employees(full_name)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_employee ON employee_salary_transactions(employee_id)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_date ON employee_salary_transactions(transaction_date)");
                db.Execute("CREATE INDEX IF NOT EXISTS idx_employee_salary_transactions_type ON employee_salary_transactions(transaction_type)");
                db.Execute("CREATE UNIQUE INDEX IF NOT EXISTS idx_users_active_employee ON users(employee_id) WHERE employee_id IS NOT NULL AND is_active = 1");

                db.SetSetting("migration007_applied", "true");
                Logger.LogInfo("Migration007: Employees and salary transactions schema applied.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Migration007 execution error");
                throw;
            }
        }
    }
}
