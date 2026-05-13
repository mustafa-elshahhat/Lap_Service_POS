using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class ReturnItem
    {
        public int Id { get; set; }
        public long ReturnId { get; set; }
        public int SaleItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
