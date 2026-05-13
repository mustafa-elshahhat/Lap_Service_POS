using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface ICustomerService
    {
        Customer GetById(int id);
        List<Customer> SearchCustomers(string query);
        List<Customer> GetAllCustomers();
        long CreateCustomer(Customer customer);
        void UpdateCustomer(Customer customer);
        void DeleteCustomer(int id);
    }
}
