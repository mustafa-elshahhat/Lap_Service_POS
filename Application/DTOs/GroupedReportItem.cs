using System.Collections.Generic;

namespace CarPartsShopWPF.Application.DTOs
{
    public class GroupedReportItem
    {
        public Dictionary<string, object> GroupHeader { get; set; }
        public List<Dictionary<string, object>> Items { get; set; }
    }
}
