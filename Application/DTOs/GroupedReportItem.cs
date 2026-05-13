using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.DTOs
{
    public class GroupedReportItem
    {
        public Dictionary<string, object> GroupHeader { get; set; }
        public List<Dictionary<string, object>> Items { get; set; }
    }
}
