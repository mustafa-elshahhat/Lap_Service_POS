using System;
using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Domain.Interfaces
{
    public interface ICustomerRepository
    {
        Customer GetById(int id);
        Customer GetByPhone(string phone);
        List<Customer> Search(string query);
        List<Customer> GetAll();
        List<Customer> GetWithCredit();
        
        long Create(Customer customer);
        void Update(Customer customer);
        void UpdateCredit(int id, decimal amountDelta);
        void Delete(int id);
    }
}
