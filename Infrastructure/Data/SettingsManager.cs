using System.Collections.Generic;

namespace AlJohary.ServiceHub.Infrastructure.Data
{
    public class SettingsManager
    {
        private readonly SqlExecutor _sql;

        public SettingsManager(SqlExecutor sql)
        {
            _sql = sql;
        }

        public string GetSetting(string key, string defaultValue = null)
        {
            var result = _sql.FetchOne("SELECT value FROM settings WHERE key = @key",
                new Dictionary<string, object> { { "@key", key } });
            return result != null ? result["value"]?.ToString() : defaultValue;
        }

        public void SetSetting(string key, string value)
        {
            _sql.Execute(@"INSERT OR REPLACE INTO settings (key, value, updated_at) 
                          VALUES (@key, @value, datetime('now'))",
                new Dictionary<string, object>
                {
                    { "@key", key },
                    { "@value", value }
                });
        }
    }
}
