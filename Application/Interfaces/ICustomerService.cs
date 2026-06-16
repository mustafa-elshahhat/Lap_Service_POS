using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface ICustomerService
    {
        Customer GetById(int id);
        Customer GetByPhone(string phone);
        List<Customer> SearchCustomers(string query);
        List<Customer> GetAllCustomers();
        long CreateCustomer(Customer customer);
        void UpdateCustomer(Customer customer);
        void DeleteCustomer(int id);
        int? GetOrCreateCustomer(string name, string phone);
    }
}
