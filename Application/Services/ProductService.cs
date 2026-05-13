using System;
using System.Collections.Generic;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Application.Interfaces;

namespace CarPartsShopWPF.Application.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        public long Create(string code, string name, decimal purchasePrice, decimal sellingPrice,
            int quantity = 0, int? minQuantity = null,
            string supplierName = null, string category = null, string description = null)
        {
            ValidateProductData(code, name, purchasePrice, sellingPrice, quantity, minQuantity ?? 0);

            if (_repository.GetByCode(code) != null)
                throw new InvalidOperationException("كود المنتج موجود بالفعل");

            var product = new Product
            {
                Code = code,
                Name = name,
                PurchasePrice = purchasePrice,
                SellingPrice = sellingPrice,
                Quantity = quantity,
                MinQuantity = minQuantity ?? 5,
                SupplierName = supplierName,
                Category = category,
                Description = description,
                IsActive = true
            };

            return _repository.Create(product);
        }

        private void ValidateProductData(string code, string name, decimal purchasePrice, decimal sellingPrice, int quantity, int minQuantity)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("كود المنتج مطلوب");

            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("اسم المنتج مطلوب");

            if (purchasePrice < 0)
                throw new ArgumentException("سعر الشراء لا يمكن أن يكون سالباً");

            if (sellingPrice < 0)
                throw new ArgumentException("سعر البيع لا يمكن أن يكون سالباً");

            if (sellingPrice < purchasePrice)
                throw new ArgumentException("سعر البيع لا يمكن أن يكون أقل من سعر الشراء");

            if (quantity < 0)
                throw new ArgumentException("الكمية لا يمكن أن تكون سالبة");

            if (minQuantity < 0)
                throw new ArgumentException("الحد الأدنى للمخزون لا يمكن أن يكون سالباً");
        }

        public void Update(int productId, string code = null, string name = null,
            decimal? purchasePrice = null, decimal? sellingPrice = null, int? quantity = null,
            int? minQuantity = null, string supplierName = null, string category = null,
            string description = null)
        {
            var product = _repository.GetById(productId);
            if (product == null) throw new InvalidOperationException("المنتج غير موجود");

            ValidateProductData(
                code ?? product.Code,
                name ?? product.Name,
                purchasePrice ?? product.PurchasePrice,
                sellingPrice ?? product.SellingPrice,
                quantity ?? product.Quantity,
                minQuantity ?? product.MinQuantity
            );

            if (code != null && code != product.Code) 
            {
                var existing = _repository.GetByCode(code);
                if (existing != null && existing.Id != productId)
                    throw new InvalidOperationException("كود المنتج موجود بالفعل");
            }

            if (code != null) product.Code = code;
            if (name != null) product.Name = name;
            if (purchasePrice.HasValue) product.PurchasePrice = purchasePrice.Value;
            if (sellingPrice.HasValue) product.SellingPrice = sellingPrice.Value;
            if (quantity.HasValue) product.Quantity = quantity.Value;
            if (minQuantity.HasValue) product.MinQuantity = minQuantity.Value;
            if (supplierName != null) product.SupplierName = supplierName;
            if (category != null) product.Category = category;
            if (description != null) product.Description = description;

            _repository.Update(product);
        }

        public void Delete(int productId) => _repository.Delete(productId);
        
        public Product GetById(int productId) => _repository.GetById(productId);
        public Product GetByCode(string code) => _repository.GetByCode(code);
        
        public List<Product> Search(string query, int limit = 50) => _repository.Search(query, limit);
        public List<Product> GetAll(bool includeInactive = false) => _repository.GetAll(includeInactive);
        public List<Product> GetLowStock() => _repository.GetLowStock();
        public List<Product> GetOutOfStock() => _repository.GetOutOfStock();
        
        public void UpdateQuantity(int productId, int quantityChange) => _repository.UpdateQuantity(productId, quantityChange);
        public void SetQuantity(int productId, int newQuantity) => _repository.SetQuantity(productId, newQuantity);
        
        public Dictionary<string, object> GetTotalInventoryValue() => _repository.GetInventoryValue();
        public List<string> GetCategories() => _repository.GetCategories();
        public List<string> GetSuppliers() => _repository.GetSuppliers();

        public (bool available, int currentQuantity) CheckStock(int productId, int requiredQuantity)
        {
            var product = _repository.GetById(productId);
            if (product == null) return (false, 0);

            return (product.Quantity >= requiredQuantity, product.Quantity);
        }

        public string GenerateProductCode()
        {
            try 
            {
                var products = _repository.GetAll(true); 
                int maxCode = 0;
                foreach (var p in products)
                {
                    if (int.TryParse(p.Code, out int val))
                    {
                        if (val > maxCode) maxCode = val;
                    }
                }
                return (maxCode + 1).ToString();
            }
            catch
            {
                return "1";
            }
        }

    }
}
