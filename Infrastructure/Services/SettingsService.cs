using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;

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
