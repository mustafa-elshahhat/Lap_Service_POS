using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AlJohary.ServiceHub.Presentation.ViewModels
{

    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event System.EventHandler RequestSearchFocus;
        protected void OnRequestSearchFocus() => RequestSearchFocus?.Invoke(this, System.EventArgs.Empty);

        public event PropertyChangedEventHandler PropertyChanged;

        public Application.Services.LanguageService Language => Application.Services.LanguageService.Instance;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public System.Windows.Input.ICommand CopyTextCommand => new RelayCommand<string>(CopyText);

        protected void CopyText(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                try { System.Windows.Clipboard.SetText(text); } catch { }
            }
        }
    }
}

