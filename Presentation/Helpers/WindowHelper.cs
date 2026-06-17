using System.Linq;
using System.Windows;
using Microsoft.Win32;

namespace AlJohary.ServiceHub.Presentation.Helpers
{
    /// <summary>
    /// Helpers for resolving the active application window so that native (Win32)
    /// dialogs are owned by the app and centre over it instead of appearing behind
    /// the window or on the wrong monitor. UI ownership only - no business logic.
    /// </summary>
    public static class WindowHelper
    {
        public static Window GetActiveOwner()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return null;

            return app.Windows
                       .OfType<Window>()
                       .FirstOrDefault(w => w.IsActive)
                   ?? app.MainWindow;
        }

        /// <summary>
        /// Shows a native file dialog owned by the active application window when one
        /// is available, falling back to the ownerless overload otherwise.
        /// </summary>
        public static bool? ShowDialogOwned(this CommonDialog dialog)
        {
            var owner = GetActiveOwner();
            return owner != null ? dialog.ShowDialog(owner) : dialog.ShowDialog();
        }
    }
}
