using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.DTOs
{
    public class ExcelImportResult
    {
        public List<SupplierPurchaseLineInput> Rows { get; set; } = new List<SupplierPurchaseLineInput>();
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
}
