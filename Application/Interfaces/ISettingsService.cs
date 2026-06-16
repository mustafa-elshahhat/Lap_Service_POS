using System;
using System.Collections.Generic;

namespace AlJohary.ServiceHub.Application.Interfaces
{
    public interface ISettingsService
    {
        string GetSetting(string key, string defaultValue = "");
        void SetSetting(string key, string value);
        List<string> GetShopPhones();
        void SetShopPhones(IEnumerable<string> phones);
        string GetDatabasePath();
        void RestoreDatabase(string backupPath);
    }
}
