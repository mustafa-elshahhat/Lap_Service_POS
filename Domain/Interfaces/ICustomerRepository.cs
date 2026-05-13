using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface ICustomerRepository
    {
        Customer GetById(int id);
        Customer GetByPhone(string phone);
        List<Customer> Search(string query);
        List<Customer> GetAll();
        long Create(Customer customer);
        void Update(Customer customer);
        void Delete(int id);
    }
}
