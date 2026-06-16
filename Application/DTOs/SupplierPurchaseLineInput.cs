namespace AlJohary.ServiceHub.Application.DTOs
{
    public class SupplierPurchaseLineInput
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPurchasePrice { get; set; }
    }
}
