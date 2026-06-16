using System;

namespace AlJohary.ServiceHub.Domain.Entities
{
    public class SupplierPurchaseItem
    {
        public int Id { get; set; }
        public int SupplierTransactionId { get; set; }
        public int SupplierId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPurchasePrice { get; set; }
        public decimal LineTotal { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
