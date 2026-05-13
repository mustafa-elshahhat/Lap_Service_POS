using System;

namespace AlJohary.ServiceHub.Domain.Entities
{
    public class RepairOrder
    {
        public long Id { get; set; }
        public string OrderNumber { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string TechnicianName { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public string OrderStatus { get; set; }
        public DateTime? ExpectedDelivery { get; set; }
        public DateTime IntakeDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string UserName { get; set; }
        public int DeviceCount { get; set; }
    }
}
