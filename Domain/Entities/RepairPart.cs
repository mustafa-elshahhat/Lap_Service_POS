using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class RepairPart
    {
        public long Id { get; set; }
        public long DeviceId { get; set; }
        public long OrderId { get; set; }
        public int? ProductId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
        public decimal PurchaseCost { get; set; }
        public bool IsFromInventory { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
