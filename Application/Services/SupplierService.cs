using System;
using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Application.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Application.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepo;
        private readonly IAuthService _auth;

        public SupplierService(ISupplierRepository supplierRepo, IAuthService auth)
        {
            _supplierRepo = supplierRepo;
            _auth = auth;
        }

        public List<Supplier> GetAllSuppliers()
        {
            return _supplierRepo.GetAllSuppliers();
        }

        public List<Supplier> SearchSuppliers(string query)
        {
            return _supplierRepo.SearchSuppliers(query);
        }
        
        public Supplier GetById(int id)
        {
            return _supplierRepo.GetById(id);
        }

        public void CreateSupplier(string name, string phone, string address)
        {
            var supplier = new Supplier
            {
                Name = name,
                Phone = phone,
                Address = address
            };
            _supplierRepo.CreateSupplier(supplier);
        }

        public void UpdateSupplier(int id, string name, string phone, string address)
        {
            var supplier = new Supplier
            {
                Id = id,
                Name = name,
                Phone = phone,
                Address = address
            };
            _supplierRepo.UpdateSupplier(supplier);
        }

        public void DeleteSupplier(int id)
        {
            _supplierRepo.DeleteSupplier(id);
        }

        public void AddSupplierPayment(int supplierId, decimal amount, string paymentMethod = null)
        {
            int userId = _auth.GetUserId();
            _supplierRepo.AddSupplierPayment(supplierId, amount, userId, paymentMethod);
        }

        public void AddSupplierPurchase(int supplierId, decimal amount, string paymentMethod = null)
        {
            int userId = _auth.GetUserId();
            _supplierRepo.AddSupplierPurchase(supplierId, amount, userId, paymentMethod);
        }
        public List<Dictionary<string, object>> GetSupplierTransactions(int supplierId)
        {
            return _supplierRepo.GetTransactions(supplierId);
        }
    }
}
