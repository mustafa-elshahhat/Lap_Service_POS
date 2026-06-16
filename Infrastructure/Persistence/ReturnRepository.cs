using System;
using System.Collections.Generic;
using AlJohary.ServiceHub.Domain.Entities;
using AlJohary.ServiceHub.Domain.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using AlJohary.ServiceHub.Shared.Helpers;

namespace AlJohary.ServiceHub.Infrastructure.Persistence
{
    public class ReturnRepository : IReturnRepository
    {
        private readonly DatabaseManager _db = DatabaseManager.Instance;

        private Return MapToReturn(Dictionary<string, object> row)
        {
            if (row == null) return null;
            return new Return
            {
                Id = SafeConvert.ToInt(row["id"]),
                ReturnNumber = SafeConvert.ToString(row["return_number"]),
                SaleId = SafeConvert.ToInt(row["sale_id"]),
                CustomerId = row["customer_id"] == DBNull.Value ? (int?)null : SafeConvert.ToInt(row["customer_id"]),
                UserId = SafeConvert.ToInt(row["user_id"]),
                TotalAmount = SafeConvert.ToDecimal(row["total_amount"]),
                Reason = SafeConvert.ToString(row["reason"]),
                PaymentMethod = SafeConvert.ToString(row["payment_method"]),
                ReturnDate = SafeConvert.ToDateTime(row["return_date"]) ?? DateTime.MinValue,
                InvoiceNumber = row.ContainsKey("invoice_number") ? SafeConvert.ToString(row["invoice_number"]) : null,
                CustomerName = row.ContainsKey("customer_name") ? SafeConvert.ToString(row["customer_name"]) : null,
                UserName = row.ContainsKey("user_name") ? SafeConvert.ToString(row["user_name"]) : null
            };
        }

        public long CreateReturn(string returnNumber, int saleId, int? customerId, int userId, decimal total, decimal cashRefund, decimal debtDeduction, string reason, string method)
        {
             return _db.ExecuteAndGetId(@"
                INSERT INTO returns (return_number, sale_id, customer_id, user_id, total_amount, cash_refund, debt_deduction, reason, payment_method, return_date)
                VALUES (@returnNumber, @saleId, @customerId, @userId, @totalAmount, @cashRefund, @debtDeduction, @reason, @paymentMethod, @returnDate)",
                new Dictionary<string, object>
                {
                    { "@returnNumber", returnNumber },
                    { "@saleId", saleId },
                    { "@customerId", customerId },
                    { "@userId", userId },
                    { "@totalAmount", total },
                    { "@cashRefund", cashRefund },
                    { "@debtDeduction", debtDeduction },
                    { "@reason", reason },
                    { "@paymentMethod", method },
                    { "@returnDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                });
        }

        public void AddReturnItem(long returnId, int saleItemId, int productId, string code, string name, int quantity, decimal unitPrice, decimal total)
        {
            _db.Execute(@"
                INSERT INTO return_items (return_id, sale_item_id, product_id, product_code, product_name, quantity, unit_price, total_price)
                VALUES (@returnId, @saleItemId, @pid, @pcode, @pname, @qty, @uprice, @total)",
                new Dictionary<string, object>
                {
                    { "@returnId", returnId },
                    { "@saleItemId", saleItemId },
                    { "@pid", productId },
                    { "@pcode", code },
                    { "@pname", name },
                    { "@qty", quantity },
                    { "@uprice", unitPrice },
                    { "@total", total }
                });
        }

        public List<Return> GetReturns(string query)
        {
             string sql = @"
                SELECT r.*, s.invoice_number, u.full_name as user_name
                FROM returns r
                LEFT JOIN sales s ON r.sale_id = s.id
                LEFT JOIN users u ON r.user_id = u.id";

             Dictionary<string, object> parameters = new Dictionary<string, object>();

             if (!string.IsNullOrWhiteSpace(query))
             {
                sql += " WHERE r.return_number LIKE @q OR s.invoice_number LIKE @q";
                parameters.Add("@q", $"%{query}%");
             }

             sql += " ORDER BY r.return_date DESC LIMIT 100";

             var rows = _db.FetchAll(sql, parameters);
             var list = new List<Return>();
             foreach (var row in rows) list.Add(MapToReturn(row));
             return list;
        }

        public Dictionary<string, object> GetReturnById(int id)
        {
            return _db.FetchOne(@"
                SELECT r.*, s.invoice_number, c.name as customer_name, u.full_name as user_name
                FROM returns r
                LEFT JOIN sales s ON r.sale_id = s.id
                LEFT JOIN customers c ON r.customer_id = c.id
                LEFT JOIN users u ON r.user_id = u.id
                WHERE r.id = @id",
                new Dictionary<string, object> { { "@id", id } });
        }

        public List<Dictionary<string, object>> GetReturnItems(int returnId)
        {
            return _db.FetchAll(@"
                SELECT ri.*, p.name as product_name
                FROM return_items ri
                LEFT JOIN products p ON ri.product_id = p.id
                WHERE ri.return_id = @returnId",
                new Dictionary<string, object> { { "@returnId", returnId } });
        }

        public Dictionary<int, int> GetReturnedQuantities(int saleId)
        {
            var rows = _db.FetchAll(@"
                SELECT sale_item_id, SUM(quantity) as returned_qty
                FROM return_items ri
                JOIN returns r ON ri.return_id = r.id
                WHERE r.sale_id = @saleId
                GROUP BY sale_item_id",
                new Dictionary<string, object> { { "@saleId", saleId } });

            var result = new Dictionary<int, int>();
            foreach(var row in rows)
            {
                result[SafeConvert.ToInt(row["sale_item_id"])] = SafeConvert.ToInt(row["returned_qty"]);
            }
            return result;
        }

        public List<Return> GetReturnsReport(string startDate, string endDate)
        {
            string start = startDate + " 00:00:00";
            string end = DateTime.Parse(endDate).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            var rows = _db.FetchAll(@"
                SELECT r.*, s.invoice_number, c.name as customer_name, u.full_name as user_name
                FROM returns r
                LEFT JOIN sales s ON r.sale_id = s.id
                LEFT JOIN customers c ON r.customer_id = c.id
                LEFT JOIN users u ON r.user_id = u.id
                WHERE r.return_date >= @start AND r.return_date < @end
                ORDER BY r.return_date DESC",
                new Dictionary<string, object> { { "@start", start }, { "@end", end } });

            var list = new List<Return>();
            foreach (var row in rows) list.Add(MapToReturn(row));
            return list;
        }

        public string GenerateReturnNumber()
        {
            return _db.GenerateReturnNumber();
        }
    }
}
