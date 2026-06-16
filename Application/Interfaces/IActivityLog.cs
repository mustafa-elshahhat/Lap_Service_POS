namespace AlJohary.ServiceHub.Application.Interfaces
{
    // Audit-log seam for the Application layer. Implemented in Infrastructure so
    // services no longer reach into the concrete DatabaseManager (see ISaleRepository.LogActivity
    // for the pre-existing equivalent on the sales path).
    public interface IActivityLog
    {
        void LogActivity(int userId, string action, string table, int recordId, string details);
    }
}
