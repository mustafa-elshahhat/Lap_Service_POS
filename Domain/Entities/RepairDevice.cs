using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class RepairDevice
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string DeviceType { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string Condition { get; set; }
        public string ReportedIssue { get; set; }
        public string Accessories { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal ServiceCost { get; set; }
        public decimal LaborCost { get; set; }
        public string DeviceStatus { get; set; }
        public string DiagnosisNotes { get; set; }
        public string RepairNotes { get; set; }
        public DateTime CreatedAt { get; set; }

        public decimal TotalCost => ServiceCost + LaborCost;
        public string DisplayName => $"{DeviceType} {Brand} {Model}".Trim();
    }
}
