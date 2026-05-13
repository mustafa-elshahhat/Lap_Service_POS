using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Markup;

namespace AlJohary.ServiceHub.Application.Services
{

    public class LanguageService : INotifyPropertyChanged
    {
        private static LanguageService _instance;
        public static LanguageService Instance => _instance ??= new LanguageService();

        private static bool _isMetadataOverridden = false;
        private FlowDirection _flowDirection;
        private CultureInfo _currentCulture;
        private XmlLanguage _currentLanguage;

        private LanguageService()
        {

            SetLanguage("ar-EG");
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public FlowDirection FlowDirection
        {
            get => _flowDirection;
            private set
            {
                if (_flowDirection != value)
                {
                    _flowDirection = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TextAlignment));
                    OnPropertyChanged(nameof(PrimaryHorizontalAlignment));
                }
            }
        }

        public TextAlignment TextAlignment => TextAlignment.Left;
        
        public HorizontalAlignment PrimaryHorizontalAlignment => HorizontalAlignment.Left;

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (_currentCulture != value)
                {
                    _currentCulture = value;
                    OnPropertyChanged();
                }
            }
        }

        public XmlLanguage CurrentLanguage
        {
            get => _currentLanguage;
            private set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged();
                }
            }
        }

        public void SetLanguage(string cultureCode)
        {
            try
            {
                var culture = new CultureInfo(cultureCode);

                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;

                CurrentCulture = culture;
                CurrentLanguage = XmlLanguage.GetLanguage(culture.IetfLanguageTag);

                FlowDirection = culture.TextInfo.IsRightToLeft 
                    ? FlowDirection.RightToLeft 
                    : FlowDirection.LeftToRight;

                if (!_isMetadataOverridden)
                {
                    try
                    {
                        FrameworkElement.LanguageProperty.OverrideMetadata(
                            typeof(FrameworkElement),
                            new FrameworkPropertyMetadata(CurrentLanguage));
                        _isMetadataOverridden = true;
                    }
                    catch 
                    {

                        _isMetadataOverridden = true; 
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting language: {ex.Message}");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
