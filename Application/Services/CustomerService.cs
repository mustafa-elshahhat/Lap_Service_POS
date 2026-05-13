using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;

namespace AlJohary.ServiceHub.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly ICustomerRepository _customerRepo;

        public CustomerService(ICustomerRepository customerRepo)
        {
            _customerRepo = customerRepo;
        }

        public Customer GetById(int id) => _customerRepo.GetById(id);
        public Customer GetByPhone(string phone) => _customerRepo.GetByPhone(NormalizePhone(phone ?? string.Empty));
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
        public void UpdateCustomer(Customer customer)
        {
            if (!string.IsNullOrWhiteSpace(customer.Phone))
                customer.Phone = NormalizePhone(customer.Phone);
            _customerRepo.Update(customer);
        }
        public void DeleteCustomer(int id) => _customerRepo.Delete(id);
    }
}
