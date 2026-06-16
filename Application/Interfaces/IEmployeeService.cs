using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface IEmployeeService
    {
        List<Employee> GetAllEmployees(bool includeInactive = false);
        List<Employee> GetActiveEmployees();
        Employee GetById(int id);
        long CreateEmployee(string fullName, string phone, string jobTitle, decimal baseSalary, string notes);
        void UpdateEmployee(int id, string fullName, string phone, string jobTitle, decimal baseSalary, string notes);
        void SetEmployeeActive(int id, bool isActive);
        long RegisterSalaryPayment(int employeeId, decimal amount, string paymentMethod, DateTime transactionDate, string notes);
        long RegisterDeduction(int employeeId, decimal amount, DateTime transactionDate, string notes);
        List<Dictionary<string, object>> GetSalaryTransactions(int employeeId);
    }
}
