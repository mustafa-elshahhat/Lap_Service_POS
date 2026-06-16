using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.DTOs
{
    public class SupplierPurchaseDialogResult
    {
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string PaymentMethod { get; set; }
        public List<SupplierPurchaseLineInput> Lines { get; set; } = new List<SupplierPurchaseLineInput>();
    }
}
