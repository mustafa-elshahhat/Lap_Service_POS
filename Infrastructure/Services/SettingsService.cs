using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;
using System.Collections.Generic;
using System.Linq;

namespace AlJohary.ServiceHub.Infrastructure.Services
{
    public class SettingsService : ISettingsService
    {
        public string GetSetting(string key, string defaultValue = "")
        {
            return DatabaseManager.Instance.GetSetting(key, defaultValue);
        }

        public void SetSetting(string key, string value)
        {
            DatabaseManager.Instance.SetSetting(key, value);
        }

        public List<string> GetShopPhones()
        {
            string phones = GetSetting("shop_phones", "");
            var list = SplitPhones(phones);
            if (list.Count == 0)
            {
                list = SplitPhones(GetSetting("shop_phone", ""));
            }
            return list;
        }

        public void SetShopPhones(IEnumerable<string> phones)
        {
            var normalized = (phones ?? Enumerable.Empty<string>())
                .Select(p => p?.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();

            SetSetting("shop_phones", string.Join("\n", normalized));
            SetSetting("shop_phone", normalized.FirstOrDefault() ?? "");
        }

        private static List<string> SplitPhones(string phones)
        {
            return (phones ?? "")
                .Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct()
                .ToList();
        }

        public string GetDatabasePath()
        {
            return DatabaseManager.Instance.DatabasePath;
        }

        public void RestoreDatabase(string backupPath)
        {
            DatabaseManager.Instance.Restore(backupPath);
        }
    }
}
