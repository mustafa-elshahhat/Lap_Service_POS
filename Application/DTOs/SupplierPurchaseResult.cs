namespace AlJohary.ServiceHub.Application.DTOs
{
    public class SupplierPurchaseResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public long TransactionId { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAdded { get; set; }
    }
}
