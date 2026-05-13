using System;
using System.Collections.Generic;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Persistence
{
    public class ProductRepository : IProductRepository
    {
        private readonly DatabaseManager _db;

        public ProductRepository()
        {
             _db = DatabaseManager.Instance;
        }

        private Product MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Product
            {
                Id = SafeConvert.ToInt(row["id"]),
                Code = SafeConvert.ToString(row["code"]),
                Name = SafeConvert.ToString(row["name"]),
                PurchasePrice = SafeConvert.ToDecimal(row["purchase_price"]),
                SellingPrice = SafeConvert.ToDecimal(row["selling_price"]),
                Quantity = SafeConvert.ToInt(row["quantity"]),
                MinQuantity = SafeConvert.ToInt(row["min_quantity"]),
                SupplierName = SafeConvert.ToString(row["supplier_name"]),
                Category = SafeConvert.ToString(row["category"]),
                Description = SafeConvert.ToString(row["description"]),
                IsActive = SafeConvert.ToBool(row["is_active"]),
                CreatedAt = SafeConvert.ToDateTime(row["created_at"]) ?? DateTime.MinValue,
                UpdatedAt = SafeConvert.ToDateTime(row["updated_at"]) ?? DateTime.MinValue
            };
        }

        public Product GetById(int id)
        {
            var row = _db.FetchOne("SELECT * FROM products WHERE id = @id", 
                new Dictionary<string, object> { { "@id", id } });
            return MapToEntity(row);
        }

        public Product GetByCode(string code)
        {
             var row = _db.FetchOne("SELECT * FROM products WHERE code = @code AND is_active = 1", 
                new Dictionary<string, object> { { "@code", code } });
            return MapToEntity(row);
        }

        public List<Product> Search(string query, int limit = 50)
        {
            string search = $"%{query}%";
            var rows = _db.FetchAll(@"
                SELECT * FROM products 
                WHERE is_active = 1 
                AND (
                    name          LIKE @search OR
                    code          LIKE @search OR
                    category      LIKE @search OR
                    supplier_name LIKE @search OR
                    description   LIKE @search
                )
                ORDER BY
                    CASE WHEN LOWER(code) = LOWER(@exact) THEN 0
                         WHEN LOWER(name) LIKE @search     THEN 1
                         ELSE 2
                    END,
                    name
                LIMIT @limit",
                new Dictionary<string, object>
                {
                    { "@search", search },
                    { "@exact",  query  },
                    { "@limit",  limit  }
                });

            var list = new List<Product>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<Product> GetAll(bool includeInactive = false)
        {
            string query = "SELECT * FROM products";
            if (!includeInactive) query += " WHERE is_active = 1";
            query += " ORDER BY name";
            var rows = _db.FetchAll(query);
            
            var list = new List<Product>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public long Create(Product product)
        {
            return _db.ExecuteAndGetId(@"
                INSERT INTO products (code, name, purchase_price, selling_price,
                                    quantity, min_quantity, supplier_name, category, description, is_active, created_at, updated_at)
                VALUES (@code, @name, @purchasePrice, @sellingPrice, @quantity,
                        @minQuantity, @supplierName, @category, @description, 1, datetime('now'), datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@code", product.Code },
                    { "@name", product.Name },
                    { "@purchasePrice", product.PurchasePrice },
                    { "@sellingPrice", product.SellingPrice },
                    { "@quantity", product.Quantity },
                    { "@minQuantity", product.MinQuantity },
                    { "@supplierName", product.SupplierName },
                    { "@category", product.Category },
                    { "@description", product.Description }
                });
        }

        public void Update(Product product)
        {
            _db.Execute(@"
               UPDATE products SET 
                   code = @code,
                   name = @name,
                   purchase_price = @purchasePrice,
                   selling_price = @sellingPrice,
                   quantity = @quantity,
                   min_quantity = @minQuantity,
                   supplier_name = @supplierName,
                   category = @category,
                   description = @description,
                   is_active = @isActive,
                   updated_at = datetime('now')
               WHERE id = @id",
               new Dictionary<string, object>
               {
                   { "@id", product.Id },
                   { "@code", product.Code },
                   { "@name", product.Name },
                   { "@purchasePrice", product.PurchasePrice },
                   { "@sellingPrice", product.SellingPrice },
                   { "@quantity", product.Quantity },
                   { "@minQuantity", product.MinQuantity },
                   { "@supplierName", product.SupplierName },
                   { "@category", product.Category },
                   { "@description", product.Description },
                   { "@isActive", product.IsActive ? 1 : 0 }
               });
        }

        public void UpdateQuantity(int id, int quantityDelta)
        {
             _db.Execute(@"
                UPDATE products 
                SET quantity = quantity + @change, updated_at = datetime('now')
                WHERE id = @productId",
                new Dictionary<string, object> { { "@change", quantityDelta }, { "@productId", id } });
        }

        public void SetQuantity(int id, int newQuantity)
        {
             _db.Execute(@"
                UPDATE products 
                SET quantity = @quantity, updated_at = datetime('now')
                WHERE id = @productId",
                new Dictionary<string, object> { { "@quantity", newQuantity }, { "@productId", id } });
        }

        public void Delete(int id)
        {
             _db.Execute("UPDATE products SET is_active = 0, updated_at = datetime('now') WHERE id = @productId", 
                new Dictionary<string, object> { { "@productId", id } });
        }

        public Dictionary<string, object> GetInventoryValue()
        {
            return _db.FetchOne(@"
                SELECT 
                    COALESCE(SUM(quantity * purchase_price), 0) as purchase_value,
                    COALESCE(SUM(quantity * selling_price), 0) as selling_value,
                    COUNT(*) as total_products,
                    COALESCE(SUM(quantity), 0) as total_quantity
                FROM products 
                WHERE is_active = 1");
        }

        public List<Product> GetLowStock()
        {
             var rows = _db.FetchAll(@"
                SELECT * FROM products 
                WHERE is_active = 1 AND quantity <= min_quantity
                ORDER BY quantity ASC");
             
             var list = new List<Product>();
             foreach (var row in rows) list.Add(MapToEntity(row));
             return list;
        }
        
        public List<Product> GetOutOfStock()
        {
            var rows = _db.FetchAll(@"
                SELECT * FROM products 
                WHERE is_active = 1 AND quantity = 0
                ORDER BY name");
            
            var list = new List<Product>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<string> GetCategories()
        {
            var result = _db.FetchAll(@"
                SELECT DISTINCT category FROM products 
                WHERE category IS NOT NULL AND category != '' AND is_active = 1
                ORDER BY category");

            var categories = new List<string>();
            foreach (var row in result) categories.Add(row["category"]?.ToString());
            return categories;
        }

        public List<string> GetSuppliers()
        {
            var result = _db.FetchAll(@"
                SELECT DISTINCT supplier_name FROM products 
                WHERE supplier_name IS NOT NULL AND supplier_name != '' AND is_active = 1
                ORDER BY supplier_name");

            var suppliers = new List<string>();
            foreach (var row in result) suppliers.Add(row["supplier_name"]?.ToString());
            return suppliers;
        }
    }
}
