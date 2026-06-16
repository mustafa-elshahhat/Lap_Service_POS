using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface ISupplierRepository
    {
        List<Supplier> GetAllSuppliers();
        List<Supplier> SearchSuppliers(string query);
        Supplier GetById(int id);
        
        void CreateSupplier(Supplier supplier);
        void UpdateSupplier(Supplier supplier);
        void DeleteSupplier(int id);

        void AddSupplierPayment(int supplierId, decimal amount, int userId, string paymentMethod = null);
        long AddSupplierPurchaseRow(int supplierId, decimal amount, decimal paidAmount, int itemCount, int userId, string paymentMethod, decimal balanceBefore, decimal balanceAfter);
        void AddSupplierPurchaseItem(SupplierPurchaseItem item);
        List<SupplierPurchaseItem> GetPurchaseItems(int supplierTransactionId);
        List<Dictionary<string, object>> GetTransactions(int supplierId);
    }
}
