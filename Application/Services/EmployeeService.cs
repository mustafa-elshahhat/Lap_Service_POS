using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly IEmployeeRepository _employeeRepo;
        private readonly IAuthService _auth;
        private readonly IDbTransactionManager _txManager;
        private readonly IActivityLog _activityLog;

        public EmployeeService(IEmployeeRepository employeeRepo, IAuthService auth, IDbTransactionManager txManager = null, IActivityLog activityLog = null)
        {
            _employeeRepo = employeeRepo;
            _auth = auth;
            _txManager = txManager;
            _activityLog = activityLog;
        }

        public List<Employee> GetAllEmployees(bool includeInactive = false)
        {
            EnsureAdmin();
            return _employeeRepo.GetAll(includeInactive);
        }

        public List<Employee> GetActiveEmployees()
        {
            EnsureAdmin();
            return _employeeRepo.GetActive();
        }

        public Employee GetById(int id)
        {
            EnsureAdmin();
            return _employeeRepo.GetById(id);
        }

        public long CreateEmployee(string fullName, string phone, string jobTitle, decimal baseSalary, string notes)
        {
            EnsureAdmin();
            ValidateEmployee(fullName, baseSalary);

            return _employeeRepo.Create(new Employee
            {
                FullName = fullName.Trim(),
                Phone = phone,
                JobTitle = jobTitle,
                BaseSalary = baseSalary,
                Notes = notes
            });
        }

        public void UpdateEmployee(int id, string fullName, string phone, string jobTitle, decimal baseSalary, string notes)
        {
            EnsureAdmin();
            ValidateEmployee(fullName, baseSalary);

            var existing = _employeeRepo.GetById(id);
            if (existing == null) throw new InvalidOperationException("الموظف غير موجود");

            _employeeRepo.Update(new Employee
            {
                Id = id,
                FullName = fullName.Trim(),
                Phone = phone,
                JobTitle = jobTitle,
                BaseSalary = baseSalary,
                Notes = notes
            });
        }

        public void SetEmployeeActive(int id, bool isActive)
        {
            EnsureAdmin();
            var employee = _employeeRepo.GetById(id);
            if (employee == null) throw new InvalidOperationException("الموظف غير موجود");

            _employeeRepo.SetActive(id, isActive);
        }

        // Salary payment = actual cash handed to the employee (carries a canonical payment method;
        // counts as cash-out in net_cash_flow and payment_outflows).
        public long RegisterSalaryPayment(int employeeId, decimal amount, string paymentMethod, DateTime transactionDate, string notes)
        {
            EnsureAdmin();
            ValidateTransaction(employeeId, amount);
            int? userId = GetCurrentUserIdOrNull();
            return RunSalaryWrite(
                () => _employeeRepo.AddSalaryTransaction(employeeId, "salary", amount, paymentMethod ?? PaymentMethods.Cash, transactionDate, notes, userId),
                userId, "register_salary", employeeId, $"Salary {amount} ({paymentMethod ?? PaymentMethods.Cash})");
        }

        // Deduction = cost reducer only; NOT a cash inflow. payment_method stays null on purpose.
        public long RegisterDeduction(int employeeId, decimal amount, DateTime transactionDate, string notes)
        {
            EnsureAdmin();
            ValidateTransaction(employeeId, amount);
            int? userId = GetCurrentUserIdOrNull();
            return RunSalaryWrite(
                () => _employeeRepo.AddSalaryTransaction(employeeId, "deduction", amount, null, transactionDate, notes, userId),
                userId, "register_deduction", employeeId, $"Deduction {amount}");
        }

        // Correction by reversal: posts a compensating row of the SAME type with the negated amount so
        // the period totals net out (e.g. salary 200 reversed -> -200 nets total_salary_payments to 0),
        // while the original row is retained for audit. No destructive delete.
        public long ReverseSalaryTransaction(long transactionId, string reason)
        {
            EnsureAdmin();

            var original = _employeeRepo.GetSalaryTransactionById(transactionId);
            if (original == null)
                throw new InvalidOperationException("العملية غير موجودة");

            int employeeId = SafeConvert.ToInt(original["employee_id"]);
            string type = SafeConvert.ToString(original["transaction_type"]);
            decimal amount = SafeConvert.ToDecimal(original["amount"]);
            string method = original.ContainsKey("payment_method") ? SafeConvert.ToString(original["payment_method"]) : null;
            if (string.IsNullOrEmpty(method)) method = null;

            string note = $"عكس/تصحيح للعملية رقم {transactionId}" + (string.IsNullOrWhiteSpace(reason) ? "" : $" - {reason}");
            int? userId = GetCurrentUserIdOrNull();

            return RunSalaryWrite(
                () => _employeeRepo.AddSalaryTransaction(employeeId, type, -amount, method, DateTime.Now, note, userId),
                userId, "reverse_salary_transaction", employeeId, $"Reversal of #{transactionId} ({type} {amount})");
        }

        private long RunSalaryWrite(Func<long> write, int? userId, string action, int employeeId, string details)
        {
            if (_txManager == null)
                return write();

            _txManager.BeginTransaction();
            try
            {
                long id = write();
                _activityLog.LogActivity(userId ?? 0, action, "employee_salary_transactions", (int)id,
                    $"EmployeeId={employeeId}; {details}");
                _txManager.CommitTransaction();
                return id;
            }
            catch
            {
                _txManager.RollbackTransaction();
                throw;
            }
        }

        public List<Dictionary<string, object>> GetSalaryTransactions(int employeeId)
        {
            EnsureAdmin();
            return _employeeRepo.GetSalaryTransactions(employeeId);
        }

        private void ValidateEmployee(string fullName, decimal baseSalary)
        {
            if (!EmployeeValidator.TryValidate(fullName, baseSalary, out string error))
                throw new ArgumentException(error);
        }

        private void ValidateTransaction(int employeeId, decimal amount)
        {
            if (amount <= 0)
                throw new ArgumentException("قيمة العملية يجب أن تكون أكبر من الصفر");

            var employee = _employeeRepo.GetById(employeeId);
            if (employee == null)
                throw new InvalidOperationException("الموظف غير موجود");
            if (!employee.IsActive)
                throw new InvalidOperationException("لا يمكن تسجيل راتب أو خصم لموظف غير نشط");
        }

        private int? GetCurrentUserIdOrNull()
        {
            int userId = _auth.GetUserId();
            return userId > 0 ? userId : (int?)null;
        }

        private void EnsureAdmin()
        {
            if (!_auth.IsAdmin)
                throw new UnauthorizedAccessException("ليس لديك صلاحية إدارة الموظفين");
        }
    }
}
