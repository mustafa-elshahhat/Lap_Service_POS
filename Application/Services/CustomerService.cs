using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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
        public long CreateCustomer(Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone))
                customer.Phone = NormalizePhone(customer.Phone);

            if (!string.IsNullOrWhiteSpace(customer.Phone))
            {
                var existing = _customerRepo.GetByPhone(customer.Phone);
                if (existing != null)
                    throw new InvalidOperationException($"رقم الهاتف مسجل بالفعل باسم: {existing.Name}");
            }

            return _customerRepo.Create(customer);
        }

        private static string NormalizePhone(string phone)
            => Regex.Replace(phone.Trim(), @"[\s\-]", "");
        public void UpdateCustomer(Customer customer) => _customerRepo.Update(customer);
        public void DeleteCustomer(int id) => _customerRepo.Delete(id);
    }
}
