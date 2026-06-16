using System.Collections.Generic;
using AlJohary.ServiceHub.Application.DTOs;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface ISupplierService
    {
        List<Supplier> GetAllSuppliers();
        List<Supplier> SearchSuppliers(string query);
        Supplier GetById(int id);
        void CreateSupplier(string name, string phone, string address);
        void UpdateSupplier(int id, string name, string phone, string address);
        void DeleteSupplier(int id);
        void AddSupplierPayment(int supplierId, decimal amount, string paymentMethod);
        // LEGACY / DE-SCOPED: superseded by AddSupplierPurchaseWithItems. No active caller.
        [System.Obsolete("Use AddSupplierPurchaseWithItems instead. This standalone purchase path is legacy.")]
        void AddSupplierPurchase(int supplierId, decimal amount, string paymentMethod);
        SupplierPurchaseResult AddSupplierPurchaseWithItems(int supplierId, decimal totalAmount, decimal paidAmount, string paymentMethod, List<SupplierPurchaseLineInput> lines);
        List<SupplierPurchaseItem> GetPurchaseItems(int supplierTransactionId);
        List<Dictionary<string, object>> GetSupplierTransactions(int supplierId);
    }
}
