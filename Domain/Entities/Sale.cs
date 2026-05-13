using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class Sale
    {
        public long Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string SaleType { get; set; }
        public int? CustomerId { get; set; }
        public int UserId { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal MarkupAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal Profit { get; set; }
        public string Notes { get; set; }
        public DateTime SaleDate { get; set; }
        public string PaymentMethod { get; set; }

        public string CustomerName { get; set; }
        public string UserName { get; set; }
    }
}
