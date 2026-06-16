using System.Collections.Generic;
using AlJohary.ServiceHub.Application.Interfaces;
using AlJohary.ServiceHub.Infrastructure.Data;

namespace AlJohary.ServiceHub.Infrastructure.Services
{
    // Single owner of the activity_log write. Executes through DatabaseManager's command
    // helper so it participates in the active transaction (CurrentTransaction), exactly as
    // the former DatabaseManager.LogActivity did.
    public class ActivityLog : IActivityLog
    {
        public void LogActivity(int userId, string action, string table, int recordId, string details)
        {
            DatabaseManager.Instance.Execute(
                @"INSERT INTO activity_log (user_id, action, table_name, record_id, details)
                  VALUES (@userId, @action, @tableName, @recordId, @details)",
                new Dictionary<string, object>
                {
                    { "@userId", userId },
                    { "@action", action },
                    { "@tableName", table },
                    { "@recordId", recordId },
                    { "@details", details }
                });
        }
    }
}
