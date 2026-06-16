using System;
using System.Collections.Generic;

namespace AlJohary.ServiceHub.Infrastructure.Data
{
    public class NumberGenerator
    {
        private readonly SqlExecutor _sql;
        private readonly SettingsManager _settings;

        public NumberGenerator(SqlExecutor sql, SettingsManager settings)
        {
            _sql = sql;
            _settings = settings;
        }

        public string GenerateInvoiceNumber()
        {
            string prefix = _settings.GetSetting("invoice_prefix", "INV");
            string datePart = DateTime.Now.ToString("yyyyMMdd");

            var result = _sql.FetchOne(
                @"SELECT invoice_number FROM sales 
                  WHERE invoice_number LIKE @pattern
                  ORDER BY id DESC LIMIT 1",
                new Dictionary<string, object> { { "@pattern", $"{prefix}-{datePart}-%" } });

            int seq = 1;
            if (result != null)
            {
                string lastNumber = result["invoice_number"]?.ToString();
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    string[] parts = lastNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                    {
                        seq = lastSeq + 1;
                    }
                }
            }

            return $"{prefix}-{datePart}-{seq:D4}";
        }

        public string GenerateReturnNumber()
        {
            string prefix = _settings.GetSetting("return_prefix", "RET");
            string datePart = DateTime.Now.ToString("yyyyMMdd");

            var result = _sql.FetchOne(
                @"SELECT return_number FROM returns 
                  WHERE return_number LIKE @pattern
                  ORDER BY id DESC LIMIT 1",
                new Dictionary<string, object> { { "@pattern", $"{prefix}-{datePart}-%" } });

            int seq = 1;
            if (result != null)
            {
                string lastNumber = result["return_number"]?.ToString();
                if (!string.IsNullOrEmpty(lastNumber))
                {
                    string[] parts = lastNumber.Split('-');
                    if (parts.Length >= 3 && int.TryParse(parts[parts.Length - 1], out int lastSeq))
                    {
                        seq = lastSeq + 1;
                    }
                }
            }

            return $"{prefix}-{datePart}-{seq:D4}";
        }
    }
}
