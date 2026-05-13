namespace CarPartsShopWPF.Application.DTOs
{
    public class SaleOperationResult
    {
        public long SaleId { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal Profit { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }
}
