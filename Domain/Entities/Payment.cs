using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class Payment
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public int? SaleId { get; set; }
        public string PaymentType { get; set; }
        public decimal Amount { get; set; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public int? ReceivedBy { get; set; }
        public string Notes { get; set; }
        public DateTime PaymentDate { get; set; }
    }
}
