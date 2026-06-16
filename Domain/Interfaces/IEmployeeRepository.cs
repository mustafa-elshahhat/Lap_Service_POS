using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IEmployeeRepository
    {
        List<Employee> GetAll(bool includeInactive = false);
        List<Employee> GetActive();
        Employee GetById(int id);
        long Create(Employee employee);
        void Update(Employee employee);
        void SetActive(int id, bool isActive);
        long AddSalaryTransaction(int employeeId, string transactionType, decimal amount, string paymentMethod, DateTime transactionDate, string notes, int? createdBy);
        List<Dictionary<string, object>> GetSalaryTransactions(int employeeId);
    }
}
