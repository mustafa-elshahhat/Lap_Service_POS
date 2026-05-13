using System;

namespace AlJohary.ServiceHub.Domain.Entities
{
    public class Return
    {
        public int Id { get; set; }
        public string ReturnNumber { get; set; }
        public int SaleId { get; set; }
        public int? CustomerId { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Reason { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ReturnDate { get; set; }

        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public string UserName { get; set; }
    }
}
