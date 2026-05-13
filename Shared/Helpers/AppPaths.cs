using System;
using System.IO;

namespace AlJohary.ServiceHub.Shared.Helpers
{
    /// <summary>
    /// Central resolver for runtime file/folder locations.
    ///
    /// The install directory next to the EXE must stay clean (EXE + DB only),
    /// so logs, migration safety backups, and any other noisy artifacts are
    /// stored under <c>%LocalAppData%\AlJohary\ServiceHub\</c>.
    ///
    /// The primary application database itself stays next to the EXE
    /// (handled by <see cref="AlJohary.ServiceHub.Infrastructure.Data.DatabaseManager"/>).
    /// </summary>
    public static class AppPaths
    {
        private const string CompanyFolder = "AlJohary";
        private const string AppFolder     = "ServiceHub";

        /// <summary>
        /// Root per-user data folder: <c>%LocalAppData%\AlJohary\ServiceHub\</c>.
        /// </summary>
        public static string LocalAppData
        {
            get
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(root))
                {
                    // Fallback: alongside the EXE if LocalAppData is unavailable for some reason.
                    root = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
                }
                return EnsureDir(Path.Combine(root, CompanyFolder, AppFolder));
            }
        }

        /// <summary>Folder for log files (<c>...\Logs\</c>).</summary>
        public static string Logs => EnsureDir(Path.Combine(LocalAppData, "Logs"));

        /// <summary>Folder for user backups (<c>...\Backups\</c>).</summary>
        public static string Backups => EnsureDir(Path.Combine(LocalAppData, "Backups"));

        /// <summary>Folder for safety backups taken by schema migrations.</summary>
        public static string MigrationBackups => EnsureDir(Path.Combine(Backups, "Migrations"));

        /// <summary>Full path of the application error/info log file.</summary>
        public static string LogFile => Path.Combine(Logs, "AppErrors.log");

        private static string EnsureDir(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch
            {
                // Swallow: directory creation must never crash a startup path.
            }
            return path;
        }
    }
}
