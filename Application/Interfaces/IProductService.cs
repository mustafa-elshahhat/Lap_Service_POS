using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;

namespace CarPartsShopWPF.Application.Interfaces
{
    public interface IProductService
    {
        long Create(string code, string name, decimal purchasePrice, decimal sellingPrice,
            int quantity = 0, string barcode = null, int? minQuantity = null,
            string supplierName = null, string category = null, string description = null);

        void Update(int productId, string code = null, string barcode = null, string name = null,
            decimal? purchasePrice = null, decimal? sellingPrice = null, int? quantity = null,
            int? minQuantity = null, string supplierName = null, string category = null,
            string description = null);

        void Delete(int productId);
        
        Product GetById(int productId);
        Product GetByCode(string code);
        Product GetByBarcode(string barcode);
        
        List<Product> Search(string query, int limit = 50);
        List<Product> GetAll(bool includeInactive = false);
        List<Product> GetLowStock();
        List<Product> GetOutOfStock();
        
        void UpdateQuantity(int productId, int quantityChange);
        void SetQuantity(int productId, int newQuantity);
        
        Dictionary<string, object> GetTotalInventoryValue();
        List<string> GetCategories();
        List<string> GetSuppliers();

        (bool available, int currentQuantity) CheckStock(int productId, int requiredQuantity);

        string GenerateProductCode();
        string GenerateBarcode();
    }
}
