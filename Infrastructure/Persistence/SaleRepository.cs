using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Infrastructure.Services;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Shared.Helpers;
using AlJohary.ServiceHub.Core.Accounting;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class SaleRepository : ISaleRepository
    {
        private readonly DatabaseManager _db;
        private readonly IActivityLog _activityLog;

        public SaleRepository()
        {
            _db = DatabaseManager.Instance;
            _activityLog = new ActivityLog();
        }

        private Sale MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Sale
            {
                Id = SafeConvert.ToLong(row["id"]),
                InvoiceNumber = SafeConvert.ToString(row["invoice_number"]),
                SaleType = SafeConvert.ToString(row["sale_type"]),
                CustomerId = row["customer_id"] == DBNull.Value || row["customer_id"] == null ? (int?)null : SafeConvert.ToInt(row["customer_id"]),
                UserId = SafeConvert.ToInt(row["user_id"]),
                Subtotal = SafeConvert.ToDecimal(row["subtotal"]),
                DiscountAmount = SafeConvert.ToDecimal(row["discount_amount"]),
                MarkupAmount = SafeConvert.ToDecimal(row["markup_amount"]),
                TotalAmount = SafeConvert.ToDecimal(row["total_amount"]),
                PaidAmount = SafeConvert.ToDecimal(row["paid_amount"]),
                RemainingAmount = SafeConvert.ToDecimal(row["remaining_amount"]),
                Profit = SafeConvert.ToDecimal(row["profit"]),
                Notes = row.ContainsKey("notes") ? SafeConvert.ToString(row["notes"]) : null,
                SaleDate = row.ContainsKey("sale_date") ? SafeConvert.ToDateTime(row["sale_date"]) ?? DateTime.MinValue : DateTime.MinValue,
                PaymentMethod = row.ContainsKey("payment_method") ? SafeConvert.ToString(row["payment_method"]) : null,
                CustomerName = row.ContainsKey("customer_name") ? SafeConvert.ToString(row["customer_name"]) : null,
                UserName = row.ContainsKey("user_name") ? SafeConvert.ToString(row["user_name"]) : null
            };
        }

        private SaleItem MapToItemEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;
            return new SaleItem
            {
                Id = SafeConvert.ToInt(row["id"]),
                SaleId = SafeConvert.ToLong(row["sale_id"]),
                ProductId = SafeConvert.ToInt(row["product_id"]),
                ProductCode = SafeConvert.ToString(row["product_code"]),
                ProductName = SafeConvert.ToString(row["product_name"]),
                Quantity = SafeConvert.ToInt(row["quantity"]),
                UnitPurchasePrice = SafeConvert.ToDecimal(row["unit_purchase_price"]),
                UnitSellingPrice = SafeConvert.ToDecimal(row["unit_selling_price"]),
                UnitFinalPrice = SafeConvert.ToDecimal(row["unit_final_price"]),
                DiscountAmount = SafeConvert.ToDecimal(row["discount_amount"]),
                MarkupAmount = SafeConvert.ToDecimal(row["markup_amount"]),
                TotalPrice = SafeConvert.ToDecimal(row["total_price"]),
                Profit = SafeConvert.ToDecimal(row["profit"]),
                PaidAmount = row.ContainsKey("paid_amount") ? SafeConvert.ToDecimal(row["paid_amount"]) : 0,
                RemainingAmount = row.ContainsKey("remaining_amount") ? SafeConvert.ToDecimal(row["remaining_amount"]) : SafeConvert.ToDecimal(row["total_price"])
            };
        }

        public long Create(Sale sale)
        {
             return _db.ExecuteAndGetId(@"
                INSERT INTO sales (invoice_number, sale_type, customer_id, user_id,
                                 subtotal, discount_amount, markup_amount, total_amount,
                                 paid_amount, remaining_amount, profit, notes, sale_date, payment_method)
                VALUES (@invoiceNumber, @saleType, @customerId, @userId, @subtotal,
                        @discountAmount, @markupAmount, @totalAmount, @paidAmount,
                        @remainingAmount, @profit, @notes, @saleDate, @paymentMethod)",
                new Dictionary<string, object>
                {
                    { "@invoiceNumber", sale.InvoiceNumber },
                    { "@saleType", sale.SaleType },
                    { "@customerId", sale.CustomerId },
                    { "@userId", sale.UserId },
                    { "@subtotal", sale.Subtotal },
                    { "@discountAmount", sale.DiscountAmount },
                    { "@markupAmount", sale.MarkupAmount },
                    { "@totalAmount", sale.TotalAmount },
                    { "@paidAmount", sale.PaidAmount },
                    { "@remainingAmount", sale.RemainingAmount },
                    { "@profit", sale.Profit },
                    { "@notes", sale.Notes },
                    { "@saleDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") },
                    { "@paymentMethod", sale.PaymentMethod }
                });
        }

        public void Update(Sale sale)
        {
            _db.Execute(@"
               UPDATE sales SET 
                   paid_amount = @paid,
                   remaining_amount = @remaining,
                   notes = @notes
               WHERE id = @id",
               new Dictionary<string, object>
               {
                   { "@id", sale.Id },
                   { "@paid", sale.PaidAmount },
                   { "@remaining", sale.RemainingAmount },
                   { "@notes", sale.Notes }
               });
        }

        public void updatePaymentStatus(long saleId, decimal paid, decimal remaining)
        {
            _db.Execute(@"UPDATE sales SET paid_amount = @paid, remaining_amount = @remaining WHERE id = @id",
                new Dictionary<string, object> { { "@paid", paid }, { "@remaining", remaining }, { "@id", saleId } });
        }

        public void UpdateSaleFinancials(long saleId, decimal total, decimal paid, decimal remaining, decimal profit)
        {
            _db.Execute(@"
                UPDATE sales SET 
                    total_amount = @total,
                    paid_amount = @paid, 
                    remaining_amount = @remaining,
                    profit = @profit
                WHERE id = @id",
                new Dictionary<string, object> 
                { 
                    { "@total", total },
                    { "@paid", paid }, 
                    { "@remaining", remaining }, 
                    { "@profit", profit },
                    { "@id", saleId } 
                });
        }

        public Sale GetById(long id)
        {
            var row = _db.FetchOne(@"
                SELECT s.*, c.name as customer_name, c.phone as customer_phone,
                       u.full_name as user_name
                FROM sales s
                LEFT JOIN customers c ON s.customer_id = c.id
                LEFT JOIN users u ON s.user_id = u.id
                WHERE s.id = @id",
                new Dictionary<string, object> { { "@id", id } });
            return MapToEntity(row);
        }

        public Sale GetByInvoiceNumber(string invoiceNumber)
        {
            var row = _db.FetchOne(@"
                SELECT s.*, c.name as customer_name, c.phone as customer_phone,
                       u.full_name as user_name
                FROM sales s
                LEFT JOIN customers c ON s.customer_id = c.id
                LEFT JOIN users u ON s.user_id = u.id
                WHERE s.invoice_number = @invoiceNumber",
                new Dictionary<string, object> { { "@invoiceNumber", invoiceNumber } });
            return MapToEntity(row);
        }

        public List<Sale> GetAll(int limit = 100)
        {
             var rows = _db.FetchAll($"SELECT id, invoice_number, sale_type, customer_id, user_id, subtotal, discount_amount, markup_amount, total_amount, paid_amount, remaining_amount, profit, notes, sale_date, payment_method FROM sales ORDER BY sale_date DESC LIMIT {limit}");
             var list = new List<Sale>();
             foreach(var row in rows) list.Add(MapToEntity(row));
             return list;
        }

        public List<Sale> Search(string query, int limit = 50)
        {
            string searchTerm = $"%{query}%";
            var rows = _db.FetchAll(@"
                SELECT s.*, c.name as customer_name, u.full_name as user_name
                FROM sales s
                LEFT JOIN customers c ON s.customer_id = c.id
                LEFT JOIN users u ON s.user_id = u.id
                WHERE s.invoice_number LIKE @search OR c.name LIKE @search OR c.phone LIKE @search
                ORDER BY s.sale_date DESC
                LIMIT @limit",
                new Dictionary<string, object> { { "@search", searchTerm }, { "@limit", limit } });
            
            var list = new List<Sale>();
            foreach(var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public List<Sale> GetByCustomerId(int customerId)
        {
            var rows = _db.FetchAll(@"
                SELECT id, invoice_number, sale_type, customer_id, user_id, subtotal, discount_amount, markup_amount, total_amount, paid_amount, remaining_amount, profit, notes, sale_date, payment_method FROM sales 
                WHERE customer_id = @customerId
                ORDER BY sale_date DESC",
                new Dictionary<string, object> { { "@customerId", customerId } });
            var list = new List<Sale>();
            foreach(var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        [Obsolete("Credit sales / receivables are not supported. All sales are fully paid; no unpaid sales exist.")]
        public List<Sale> GetUnpaidByCustomer(int customerId)
        {
            var rows = _db.FetchAll(@"
                SELECT * FROM sales 
                WHERE customer_id = @customerId AND remaining_amount > 0.01
                ORDER BY sale_date ASC",
                new Dictionary<string, object> { { "@customerId", customerId } });
             var list = new List<Sale>();
             foreach(var row in rows) list.Add(MapToEntity(row));
             return list;
        }

         public List<Sale> GetSalesReport(string startDate, string endDate)
        {
            string start = startDate + " 00:00:00";
            string end = DateTime.Parse(endDate).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            var rows = _db.FetchAll(@"
                SELECT s.*, c.name as customer_name, u.full_name as user_name
                FROM sales s
                LEFT JOIN customers c ON s.customer_id = c.id
                LEFT JOIN users u ON s.user_id = u.id
                WHERE s.sale_date >= @start AND s.sale_date < @end
                ORDER BY s.sale_date DESC",
                new Dictionary<string, object> { { "@start", start }, { "@end", end } });

            var list = new List<Sale>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public void UpdateSaleItemFinancials(int itemId, decimal paid, decimal remaining)
        {
            _db.Execute("UPDATE sale_items SET paid_amount = @paid, remaining_amount = @remaining WHERE id = @id",
                new Dictionary<string, object> { { "@paid", paid }, { "@remaining", remaining }, { "@id", itemId } });
        }

        [Obsolete("Unused legacy helper from the credit/receivable era. Not called anywhere.")]
        public void UpdateSaleItemFinancialsAfterReturn(int itemId, decimal newTotalPrice, decimal newProfit)
        {
            _db.Execute("UPDATE sale_items SET total_price = @total, profit = @profit WHERE id = @id",
                new Dictionary<string, object> { { "@total", newTotalPrice }, { "@profit", newProfit }, { "@id", itemId } });
        }

        public void AddSaleItem(long saleId, SaleItem item)
        {
            _db.Execute(@"
                INSERT INTO sale_items (sale_id, product_id, product_code, product_name,
                                       quantity, unit_purchase_price, unit_selling_price,
                                       unit_final_price, discount_amount, markup_amount,
                                       total_price, profit, paid_amount, remaining_amount)
                VALUES (@saleId, @productId, @productCode, @productName,
                        @quantity, @unitPurchasePrice, @unitSellingPrice,
                        @unitFinalPrice, @discountAmount, @markupAmount,
                        @totalPrice, @profit, @paid, @remaining)",
                new Dictionary<string, object>
                {
                    { "@saleId", saleId },
                    { "@productId", item.ProductId },
                    { "@productCode", item.ProductCode },
                    { "@productName", item.ProductName },
                    { "@quantity", item.Quantity },
                    { "@unitPurchasePrice", item.UnitPurchasePrice },
                    { "@unitSellingPrice", item.UnitSellingPrice },
                    { "@unitFinalPrice", item.UnitFinalPrice },
                    { "@discountAmount", item.DiscountAmount },
                    { "@markupAmount", item.MarkupAmount },
                    { "@totalPrice", item.TotalPrice > 0 ? item.TotalPrice : (item.Quantity * item.UnitFinalPrice) },
                    { "@profit", item.Profit },
                    { "@paid", item.PaidAmount },
                    { "@remaining", item.RemainingAmount }
                });
        }

        public List<SaleItem> GetItems(long saleId)
        {
            var rows = _db.FetchAll("SELECT id, sale_id, product_id, product_code, product_name, quantity, unit_purchase_price, unit_selling_price, unit_final_price, discount_amount, markup_amount, total_price, profit, paid_amount, remaining_amount FROM sale_items WHERE sale_id = @saleId",
                new Dictionary<string, object> { { "@saleId", saleId } });
            
            var list = new List<SaleItem>();
            foreach(var row in rows) list.Add(MapToItemEntity(row));
            return list;
        }
        

        
        public void UpdateItemPayment(int itemId, decimal newPaidAmount)
        {
            _db.Execute("UPDATE sale_items SET paid_amount = @paid WHERE id = @id",
                new Dictionary<string, object> { { "@paid", newPaidAmount }, { "@id", itemId } });
        }

        public void AddSalePayment(long saleId, string method, decimal amount, string notes)
        {
            _db.Execute(@"
                INSERT INTO sale_payments (sale_id, payment_method, amount, notes, payment_date)
                VALUES (@saleId, @method, @amount, @notes, @paymentDate)",
                new Dictionary<string, object>
                {
                    { "@saleId", saleId },
                    { "@method", method },
                    { "@amount", amount },
                    { "@notes", notes },
                    { "@paymentDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                });
        }

        public List<Dictionary<string, object>> GetSalePayments(long saleId)
        {
            return _db.FetchAll("SELECT id, sale_id, payment_method, amount, notes, payment_date FROM sale_payments WHERE sale_id = @saleId ORDER BY payment_date DESC",
                new Dictionary<string, object> { { "@saleId", saleId } });
        }

        public List<Sale> GetTodaySales()
        {
            string start = DateTime.Today.ToString("yyyy-MM-dd") + " 00:00:00";
            string end = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            var rows = _db.FetchAll(@"
                SELECT s.*, c.name as customer_name, u.full_name as user_name
                FROM sales s
                LEFT JOIN customers c ON s.customer_id = c.id
                LEFT JOIN users u ON s.user_id = u.id
                WHERE s.sale_date >= @start AND s.sale_date < @end
                ORDER BY s.sale_date DESC",
                new Dictionary<string, object> { { "@start", start }, { "@end", end } });

            var list = new List<Sale>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        [Obsolete("Use IReportRepository.GetDailySummary / GetPeriodSummary for the full financial summary.")]
        public Dictionary<string, object> GetDailySummary(DateTime date)
        {
            string start = date.ToString("yyyy-MM-dd") + " 00:00:00";
            string end = date.AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            return _db.FetchOne(@"
                SELECT
                    COUNT(*) as total_sales,
                    COALESCE(SUM(total_amount), 0) as total_revenue,
                    COALESCE(SUM(profit), 0) as total_profit
                FROM sales
                WHERE sale_date >= @start AND sale_date < @end",
                new Dictionary<string, object> { { "@start", start }, { "@end", end } });
        }

        [Obsolete("Use IReportRepository.GetPeriodSummary for the full period financial summary.")]
        public Dictionary<string, object> GetMonthlySummary(DateTime startDate, DateTime endDate)
        {
            string start = startDate.ToString("yyyy-MM-dd") + " 00:00:00";
            string end = endDate.AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            return _db.FetchOne(@"
                SELECT
                    COUNT(*) as total_sales,
                    COALESCE(SUM(total_amount), 0) as total_revenue,
                    COALESCE(SUM(remaining_amount), 0) as total_credit,
                    COALESCE(SUM(profit), 0) as total_profit
                FROM sales
                WHERE sale_date >= @start AND sale_date < @end",
                new Dictionary<string, object> { { "@start", start }, { "@end", end } });
        }

        public string GenerateInvoiceNumber()
        {
            return _db.GenerateInvoiceNumber();
        }

        public void LogActivity(int userId, string action, string table, int recordId, string details)
        {
            _activityLog.LogActivity(userId, action, table, recordId, details);
        }
    }
}
