using System.Collections.Generic;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;

namespace CarPartsShopWPF.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;

        public CustomerService(ICustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }

        public Customer GetById(int id) => _customerRepo.GetById(id);
        public List<Customer> SearchCustomers(string query) => _customerRepo.Search(query);
        public List<Customer> GetAllCustomers() => _customerRepo.GetAll();
        public List<Customer> GetCustomersWithCredit() => _customerRepo.GetWithCredit();
        public void CreateCustomer(Customer customer) => _customerRepo.Create(customer);
        public void UpdateCustomer(Customer customer) => _customerRepo.Update(customer);
        public void DeleteCustomer(int id) => _customerRepo.Delete(id);
    }
}
