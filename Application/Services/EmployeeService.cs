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

        public EmployeeService(IEmployeeRepository employeeRepo, IAuthService auth)
        {
            _employeeRepo = employeeRepo;
            _auth = auth;
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

        public long RegisterSalaryPayment(int employeeId, decimal amount, string paymentMethod, DateTime transactionDate, string notes)
        {
            EnsureAdmin();
            ValidateTransaction(employeeId, amount);
            return _employeeRepo.AddSalaryTransaction(employeeId, "salary", amount, paymentMethod ?? PaymentMethods.Cash, transactionDate, notes, GetCurrentUserIdOrNull());
        }

        public long RegisterDeduction(int employeeId, decimal amount, DateTime transactionDate, string notes)
        {
            EnsureAdmin();
            ValidateTransaction(employeeId, amount);
            return _employeeRepo.AddSalaryTransaction(employeeId, "deduction", amount, null, transactionDate, notes, GetCurrentUserIdOrNull());
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
