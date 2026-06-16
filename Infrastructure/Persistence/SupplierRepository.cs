using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly DatabaseManager _db;

        public SupplierRepository()
        {
             _db = DatabaseManager.Instance;
        }

        private Supplier MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Supplier
            {
                Id = SafeConvert.ToInt(row["id"]),
                Name = SafeConvert.ToString(row["name"]),
                Phone = SafeConvert.ToString(row["phone"]),
                Address = SafeConvert.ToString(row["address"]),
                TotalDebt = SafeConvert.ToDecimal(row["total_debt"]),
                CreatedAt = SafeConvert.ToDateTime(row["created_at"]) ?? DateTime.MinValue,
                UpdatedAt = SafeConvert.ToDateTime(row["updated_at"]) ?? DateTime.MinValue
            };
        }

        public List<Supplier> GetAllSuppliers()
        {
            var rows = _db.FetchAll("SELECT id, name, phone, address, total_debt, created_at, updated_at FROM suppliers WHERE is_active = 1 ORDER BY name");
            var list = new List<Supplier>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<Supplier> SearchSuppliers(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAllSuppliers();

            var rows = _db.FetchAll(@"
                SELECT id, name, phone, address, total_debt, created_at, updated_at FROM suppliers 
                WHERE is_active = 1 AND (name LIKE @search OR phone LIKE @search)
                ORDER BY name",
                new Dictionary<string, object> { { "@search", $"%{query}%" } });
            
            var list = new List<Supplier>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public Supplier GetById(int id)
        {
             var row = _db.FetchOne("SELECT id, name, phone, address, total_debt, created_at, updated_at FROM suppliers WHERE id = @id",
                new Dictionary<string, object> { { "@id", id } });
             return MapToEntity(row);
        }

        public void CreateSupplier(Supplier supplier)
        {
            _db.Execute(@"INSERT INTO suppliers (name, phone, address, total_debt, is_active, created_at, updated_at) 
                          VALUES (@name, @phone, @address, 0, 1, datetime('now'), datetime('now'))",
                new Dictionary<string, object> 
                { 
                    { "@name", supplier.Name },
                    { "@phone", supplier.Phone },
                    { "@address", supplier.Address }
                });
        }

        public void UpdateSupplier(Supplier supplier)
        {
            _db.Execute(@"UPDATE suppliers SET name = @name, phone = @phone, address = @address, updated_at = datetime('now') WHERE id = @id",
                new Dictionary<string, object>
                {
                    { "@name", supplier.Name },
                    { "@phone", supplier.Phone },
                    { "@address", supplier.Address },
                    { "@id", supplier.Id }
                });
        }

        public void DeleteSupplier(int id)
        {
            _db.Execute(@"UPDATE suppliers SET is_active = 0, updated_at = datetime('now') WHERE id = @id",
                new Dictionary<string, object> { { "@id", id } });
        }

        public void AddSupplierPayment(int supplierId, decimal amount, int userId, string paymentMethod = null)
        {
            var supplier = _db.FetchOne("SELECT total_debt FROM suppliers WHERE id = @id",
                new Dictionary<string, object> { { "@id", supplierId } });

            if (supplier == null)
                throw new Exception("المورد غير موجود");

            decimal currentDebt = SafeConvert.ToDecimal(supplier["total_debt"]);
            if (amount > currentDebt)
                throw new InvalidOperationException("قيمة السداد تتجاوز المديونية الحالية للمورد");

            decimal balanceBefore = currentDebt;
            decimal balanceAfter = currentDebt - amount;

            _db.Execute(@"
                INSERT INTO supplier_transactions
                (supplier_id, transaction_type, amount, transaction_date, payment_method, balance_before, balance_after, created_by)
                VALUES (@supplierId, 'payment', @amount, @transactionDate, @paymentMethod, @balanceBefore, @balanceAfter, @userId)",
                new Dictionary<string, object>
                {
                    { "@supplierId", supplierId },
                    { "@amount", amount },
                    { "@transactionDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@paymentMethod", paymentMethod ?? "نقدي" },
                    { "@balanceBefore", balanceBefore },
                    { "@balanceAfter", balanceAfter },
                    { "@userId", userId }
                });

            _db.Execute(@"UPDATE suppliers SET total_debt = total_debt - @amount, updated_at = datetime('now') WHERE id = @supplierId",
                new Dictionary<string, object>
                {
                    { "@amount", amount },
                    { "@supplierId", supplierId }
                });
        }

        public long AddSupplierPurchaseRow(int supplierId, decimal amount, decimal paidAmount, int itemCount, int userId, string paymentMethod, decimal balanceBefore, decimal balanceAfter)
        {
            long transactionId = _db.ExecuteAndGetId(@"
                INSERT INTO supplier_transactions
                (supplier_id, transaction_type, amount, transaction_date, payment_method, paid_amount, item_count, balance_before, balance_after, created_by)
                VALUES (@supplierId, 'purchase', @amount, @transactionDate, @paymentMethod, @paidAmount, @itemCount, @balanceBefore, @balanceAfter, @userId)",
                new Dictionary<string, object>
                {
                    { "@supplierId", supplierId },
                    { "@amount", amount },
                    { "@transactionDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@paymentMethod", paymentMethod ?? "نقدي" },
                    { "@paidAmount", paidAmount },
                    { "@itemCount", itemCount },
                    { "@balanceBefore", balanceBefore },
                    { "@balanceAfter", balanceAfter },
                    { "@userId", userId }
                });

            _db.Execute(@"UPDATE suppliers SET total_debt = @balanceAfter, updated_at = datetime('now') WHERE id = @supplierId",
                new Dictionary<string, object>
                {
                    { "@balanceAfter", balanceAfter },
                    { "@supplierId", supplierId }
                });

            return transactionId;
        }

        public void AddSupplierPurchaseItem(SupplierPurchaseItem item)
        {
            _db.Execute(@"
                INSERT INTO supplier_purchase_items
                (supplier_transaction_id, supplier_id, product_name, quantity, unit_purchase_price, line_total)
                VALUES (@transactionId, @supplierId, @productName, @quantity, @unitPurchasePrice, @lineTotal)",
                new Dictionary<string, object>
                {
                    { "@transactionId", item.SupplierTransactionId },
                    { "@supplierId", item.SupplierId },
                    { "@productName", item.ProductName },
                    { "@quantity", item.Quantity },
                    { "@unitPurchasePrice", item.UnitPurchasePrice },
                    { "@lineTotal", item.LineTotal }
                });
        }

        public List<SupplierPurchaseItem> GetPurchaseItems(int supplierTransactionId)
        {
            var rows = _db.FetchAll(@"
                SELECT id, supplier_transaction_id, supplier_id, product_name, quantity, unit_purchase_price, line_total, created_at
                FROM supplier_purchase_items
                WHERE supplier_transaction_id = @transactionId
                ORDER BY id",
                new Dictionary<string, object> { { "@transactionId", supplierTransactionId } });

            var items = new List<SupplierPurchaseItem>();
            foreach (var row in rows)
            {
                items.Add(new SupplierPurchaseItem
                {
                    Id = SafeConvert.ToInt(row["id"]),
                    SupplierTransactionId = SafeConvert.ToInt(row["supplier_transaction_id"]),
                    SupplierId = SafeConvert.ToInt(row["supplier_id"]),
                    ProductName = SafeConvert.ToString(row["product_name"]),
                    Quantity = SafeConvert.ToInt(row["quantity"]),
                    UnitPurchasePrice = SafeConvert.ToDecimal(row["unit_purchase_price"]),
                    LineTotal = SafeConvert.ToDecimal(row["line_total"]),
                    CreatedAt = SafeConvert.ToDateTime(row["created_at"]) ?? DateTime.MinValue
                });
            }
            return items;
        }

        public List<Dictionary<string, object>> GetTransactions(int supplierId)
        {
            return _db.FetchAll(@"
                SELECT t.*, u.full_name as created_by
                FROM supplier_transactions t
                LEFT JOIN users u ON t.created_by = u.id
                WHERE t.supplier_id = @supplierId
                ORDER BY t.transaction_date DESC, t.id DESC
                LIMIT 200", 
                new Dictionary<string, object> { { "@supplierId", supplierId } });
        }
    }
}
