using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;

namespace AlJohary.ServiceHub.Domain.Interfaces
{
    public interface IProductRepository
    {
        Product GetById(int id);
        Product GetByCode(string code);
        List<Product> Search(string query, int limit = 50);
        List<Product> GetAll(bool includeInactive = false);
        
        long Create(Product product);
        void Update(Product product);
        void Delete(int id);

        void UpdateQuantity(int id, int quantityDelta);
        void SetQuantity(int id, int newQuantity);

        Dictionary<string, object> GetInventoryValue(); 
        
        List<Product> GetLowStock();
        List<Product> GetOutOfStock();
        
        List<string> GetCategories();
        List<string> GetSuppliers();
    }
}
