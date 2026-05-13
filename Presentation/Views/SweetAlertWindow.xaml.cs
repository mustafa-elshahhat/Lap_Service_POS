using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace AlJohary.ServiceHub.Presentation.Views
{

    public partial class SweetAlertWindow : Window
    {
        public enum AlertType
        {
            Success,
            Error,
            Warning,
            Info,
            Question
        }

        public bool IsConfirmed { get; private set; } = false;

        private DispatcherTimer _autoCloseTimer;
        private double _autoCloseTotalMs = 0;
        private double _autoCloseElapsedMs = 0;

        public SweetAlertWindow(string title, string message, AlertType type, bool showCancel = false, string confirmText = "موافق", string cancelText = "إلغاء", int autoCloseSeconds = 0)
        {
            InitializeComponent();
            
            TitleText.Text = title;
            MessageText.Text = message;
            ConfirmButton.Content = confirmText;
            CancelButton.Content = cancelText;

            if (showCancel)
            {
                CancelButton.Visibility = Visibility.Visible;
                ConfirmButton.MinWidth = 100;
                CancelButton.MinWidth = 100;
            }
            else
            {
                CancelButton.Visibility = Visibility.Collapsed;
                ConfirmButton.MinWidth = 140;
            }

            ApplyTheme(type);

            if (autoCloseSeconds > 0 && !showCancel)
            {
                _autoCloseTotalMs = autoCloseSeconds * 1000.0;
                _autoCloseElapsedMs = 0;
                ProgressBarBackground.Visibility = Visibility.Visible;
                ProgressFill.Width = 0;

                _autoCloseTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                _autoCloseTimer.Tick += (s, e) =>
                {
                    _autoCloseElapsedMs += _autoCloseTimer.Interval.TotalMilliseconds;
                    var frac = Math.Min(1.0, _autoCloseElapsedMs / _autoCloseTotalMs);

                    if (ProgressBarBackground.ActualWidth > 0)
                    {
                        ProgressFill.Width = ProgressBarBackground.ActualWidth * frac;
                    }

                    if (frac >= 1.0)
                    {
                        _autoCloseTimer.Stop();
                        try { DialogResult = false; } catch { }
                        Close();
                    }
                };
                this.Closed += (s, e) => { _autoCloseTimer?.Stop(); };
                _autoCloseTimer.Start();
            }
        }

        private void ApplyTheme(AlertType type)
        {
            switch (type)
            {
                case AlertType.Success:
                    IconText.Text = "✓";
                    IconText.Foreground = (Brush)System.Windows.Application.Current.Resources["SecondaryBrush"];
                    IconBorder.BorderBrush = (Brush)System.Windows.Application.Current.Resources["SecondaryBrush"];
                    ConfirmButton.Style = (Style)System.Windows.Application.Current.Resources["SecondaryButton"];
                    break;

                case AlertType.Error:
                    IconText.Text = "✕";
                    IconText.Foreground = (Brush)System.Windows.Application.Current.Resources["DangerBrush"];
                    IconBorder.BorderBrush = (Brush)System.Windows.Application.Current.Resources["DangerBrush"];
                    ConfirmButton.Style = (Style)System.Windows.Application.Current.Resources["DangerButton"];
                    break;

                case AlertType.Warning:
                    IconText.Text = "!";
                    IconText.Foreground = (Brush)System.Windows.Application.Current.Resources["WarningBrush"];
                    IconBorder.BorderBrush = (Brush)System.Windows.Application.Current.Resources["WarningBrush"];
                    ConfirmButton.Style = (Style)System.Windows.Application.Current.Resources["WarningButton"];
                    break;

                case AlertType.Info:
                    IconText.Text = "ℹ";
                    IconText.Foreground = (Brush)System.Windows.Application.Current.Resources["InfoBrush"];
                    IconBorder.BorderBrush = (Brush)System.Windows.Application.Current.Resources["InfoBrush"];
                    ConfirmButton.Style = (Style)System.Windows.Application.Current.Resources["InfoButton"];
                    break;

                case AlertType.Question:
                    IconText.Text = "?";
                    IconText.Foreground = (Brush)System.Windows.Application.Current.Resources["PrimaryBrush"];
                    IconBorder.BorderBrush = (Brush)System.Windows.Application.Current.Resources["PrimaryBrush"];
                    ConfirmButton.Style = (Style)System.Windows.Application.Current.Resources["PrimaryButton"];
                    break;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            IsConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _autoCloseTimer?.Stop();
            IsConfirmed = false;
            DialogResult = false;
            Close();
        }
    }
}
