namespace AlJohary.ServiceHub.Domain.Entities
{
    public class SaleItem
    {
        public int Id { get; set; }
        public long SaleId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPurchasePrice { get; set; }
        public decimal UnitSellingPrice { get; set; }
        public decimal UnitFinalPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal MarkupAmount { get; set; }
        
        public decimal TotalPrice { get; set; }
        public decimal Profit { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
    }
}
