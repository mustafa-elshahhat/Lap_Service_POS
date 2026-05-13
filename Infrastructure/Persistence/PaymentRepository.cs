using System;
using System.Collections.Generic;
using CarPartsShopWPF.Infrastructure.Data;
using CarPartsShopWPF.Domain.Entities;
using CarPartsShopWPF.Domain.Interfaces;
using CarPartsShopWPF.Shared.Helpers;

namespace CarPartsShopWPF.Infrastructure.Persistence
{
    public class PaymentRepository : IPaymentRepository
    {
        private readonly DatabaseManager _db;

        public PaymentRepository()
        {
             _db = DatabaseManager.Instance;
        }

        private Payment MapToEntity(Dictionary<string, object> row)
        {
            if (row == null) return null;

            return new Payment
            {
                Id = SafeConvert.ToInt(row["id"]),
                CustomerId = SafeConvert.ToInt(row["customer_id"]),
                SaleId = row.ContainsKey("sale_id") && row["sale_id"] != DBNull.Value ? SafeConvert.ToInt(row["sale_id"]) : (int?)null,
                PaymentType = SafeConvert.ToString(row["payment_type"]),
                Amount = SafeConvert.ToDecimal(row["amount"]),
                BalanceBefore = SafeConvert.ToDecimal(row["balance_before"]),
                BalanceAfter = SafeConvert.ToDecimal(row["balance_after"]),
                ReceivedBy = row.ContainsKey("received_by") && row["received_by"] != DBNull.Value ? SafeConvert.ToInt(row["received_by"]) : (int?)null,
                Notes = SafeConvert.ToString(row["notes"]),
                PaymentDate = SafeConvert.ToDateTime(row["payment_date"]) ?? DateTime.MinValue
            };
        }

        public void AddPaymentHistory(Payment payment)
        {
            _db.Execute(@"
                INSERT INTO payment_history (customer_id, sale_id, payment_type, amount,
                                            balance_before, balance_after, received_by, notes, payment_date)
                VALUES (@customerId, @saleId, @paymentType, @amount, @balanceBefore, @balanceAfter, @receivedBy, @notes, @paymentDate)",
                new Dictionary<string, object>
                {
                    { "@customerId", payment.CustomerId },
                    { "@saleId", payment.SaleId },
                    { "@paymentType", payment.PaymentType },
                    { "@amount", payment.Amount },
                    { "@balanceBefore", payment.BalanceBefore },
                    { "@balanceAfter", payment.BalanceAfter },
                    { "@receivedBy", payment.ReceivedBy },
                    { "@notes", payment.Notes },
                    { "@paymentDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                });
        }

        public List<Payment> GetPaymentHistory(int customerId)
        {
            var rows = _db.FetchAll(@"
                SELECT ph.*, s.invoice_number, u.full_name as received_by_name
                FROM payment_history ph
                LEFT JOIN sales s ON ph.sale_id = s.id
                LEFT JOIN users u ON ph.received_by = u.id
                WHERE ph.customer_id = @customerId
                ORDER BY ph.payment_date DESC",
                new Dictionary<string, object> { { "@customerId", customerId } });
            
            var list = new List<Payment>();
            foreach (var row in rows) list.Add(MapToEntity(row));
            return list;
        }

        public Dictionary<string, decimal> GetPaymentBreakdown(long saleId)
        {
            var payments = new Dictionary<string, decimal>();

            var initial = _db.FetchAll(@"
                SELECT payment_method, SUM(amount) as total
                FROM sale_payments WHERE sale_id = @saleId GROUP BY payment_method",
                new Dictionary<string, object> { { "@saleId", saleId } });

            foreach (var item in initial)
            {
                string method = SafeConvert.ToString(item["payment_method"]) ?? "نقدي";
                decimal amount = SafeConvert.ToDecimal(item["total"]);
                if (payments.ContainsKey(method)) payments[method] += amount;
                else payments[method] = amount;
            }
            
            return payments;
        }

        public List<Payment> GetPaymentsByDateRange(string startDate, string endDate)
        {
            string start = startDate + " 00:00:00";
            string end = DateTime.Parse(endDate).AddDays(1).ToString("yyyy-MM-dd") + " 00:00:00";
            var rows = _db.FetchAll(@"
                SELECT ph.*, c.name as customer_name, s.invoice_number, u.full_name as received_by_name
                FROM payment_history ph
                LEFT JOIN customers c ON ph.customer_id = c.id
                LEFT JOIN sales s ON ph.sale_id = s.id
                LEFT JOIN users u ON ph.received_by = u.id
                WHERE ph.payment_date >= @start AND ph.payment_date < @end
                ORDER BY ph.payment_date DESC",
                new Dictionary<string, object>
                {
                    { "@start", start },
                    { "@end", end }
                });
            
            var list = new List<Payment>();
            foreach(var row in rows) list.Add(MapToEntity(row));
            return list;
        }
    }
}
