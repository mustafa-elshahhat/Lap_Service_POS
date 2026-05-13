using System;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface ISettingsService
    {
        string GetSetting(string key, string defaultValue = "");
        void SetSetting(string key, string value);
        string GetDatabasePath();
        void RestoreDatabase(string backupPath);
    }
}
