using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class RepairPayment
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentDate { get; set; }
        public string Notes { get; set; }
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
