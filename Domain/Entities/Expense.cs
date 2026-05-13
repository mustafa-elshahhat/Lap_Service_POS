using System;

namespace CarPartsShopWPF.Domain.Entities
{
    public class Expense
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public string Category { get; set; }
        public int UserId { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime ExpenseDate { get; set; }
        public string UserName { get; set; }
    }
}
