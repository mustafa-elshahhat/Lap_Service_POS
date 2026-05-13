using System;
using System.Collections.Generic;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Persistence
{
    public class CustomerRepository : ICustomerRepository
    {
        private readonly DatabaseManager _db;

        public CustomerRepository()
        {
             _db = DatabaseManager.Instance;
        }

        private Customer MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Customer
            {
                Id = SafeConvert.ToInt(row["id"]),
                Name = SafeConvert.ToString(row["name"]),
                Phone = SafeConvert.ToString(row["phone"]),
                Address = SafeConvert.ToString(row["address"]),
                Notes = SafeConvert.ToString(row["notes"]),
                CreatedAt = SafeConvert.ToDateTime(row["created_at"]) ?? DateTime.MinValue,
                UpdatedAt = SafeConvert.ToDateTime(row["updated_at"]) ?? DateTime.MinValue
            };
        }

        public long Create(Customer customer)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO customers (name, phone, address, notes, total_credit, created_at, updated_at)
                VALUES (@name, @phone, @address, @notes, 0, datetime('now'), datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@name", customer.Name },
                    { "@phone", customer.Phone },
                    { "@address", customer.Address },
                    { "@notes", customer.Notes }
                });
        }

        public void Update(Customer customer)
        {
            _db.Execute(@"
               UPDATE customers SET 
                   name = @name,
                   phone = @phone,
                   address = @address,
                   notes = @notes,
                   updated_at = datetime('now')
               WHERE id = @id",
               new Dictionary<string, object>
               {
                   { "@id", customer.Id },
                   { "@name", customer.Name },
                   { "@phone", customer.Phone },
                   { "@address", customer.Address },
                   { "@notes", customer.Notes }
               });
        }

        public void Delete(int id)
        {
            _db.Execute("DELETE FROM customers WHERE id = @id",
                new Dictionary<string, object> { { "@id", id } });
        }

        public Customer GetById(int id)
        {
            var row = _db.FetchOne("SELECT * FROM customers WHERE id = @id",
                new Dictionary<string, object> { { "@id", id } });
            return MapToEntity(row);
        }

        public Customer GetByPhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return null;
            var row = _db.FetchOne("SELECT * FROM customers WHERE phone = @phone",
                new Dictionary<string, object> { { "@phone", phone } });
            return MapToEntity(row);
        }

        public List<Customer> Search(string query)
        {
            string searchTerm = $"%{query}%";
            var rows = _db.FetchAll(@"
                SELECT * FROM customers 
                WHERE name LIKE @search OR phone LIKE @search
                ORDER BY name",
                new Dictionary<string, object> { { "@search", searchTerm } });
            
            var list = new List<Customer>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<Customer> GetAll()
        {
            var rows = _db.FetchAll("SELECT * FROM customers ORDER BY name");
            var list = new List<Customer>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

    }
}
