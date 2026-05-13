using System;

namespace CarPartsShopWPF.Application.DTOs
{
    public class RepairOrderInput
    {
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string TechnicianName { get; set; }
        public DateTime? ExpectedDelivery { get; set; }
        public string Notes { get; set; }
        public decimal InitialPayment { get; set; }
        public string InitialPaymentMethod { get; set; }
    }

    public class RepairDeviceInput
    {
        public string DeviceType { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string SerialNumber { get; set; }
        public string Condition { get; set; }
        public string ReportedIssue { get; set; }
        public string Accessories { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal LaborCost { get; set; }
        public string DeviceStatus { get; set; }
        public string DiagnosisNotes { get; set; }
        public string RepairNotes { get; set; }
    }
}
