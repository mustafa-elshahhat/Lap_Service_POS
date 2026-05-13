using System;
using System.Windows;
using System.Windows.Input;
using CarPartsShopWPF.Presentation.ViewModels;
using CarPartsShopWPF.Shared.Helpers;
using CarPartsShopWPF.Presentation.Helpers;

namespace CarPartsShopWPF.Presentation.Views
{

    public partial class MainWindow : Window
    {
        private WindowResizer _windowResizer;

        private bool _isFullScreen = false;
        private WindowState _previousWindowState;
        private double _previousWidth;
        private double _previousHeight;
        private double _previousLeft;
        private double _previousTop;

        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            vm.CloseAction = () => this.Close();
            this.DataContext = vm;
            
            vm.Initialize();
            _windowResizer = new WindowResizer(this);
            _windowResizer.Register();

            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && _isFullScreen)
            {
                ToggleFullScreen();
                e.Handled = true;
            }
        }

        private void ToggleFullScreen()
        {
            if (!_isFullScreen)
            {
                _isFullScreen = true;

                _previousWindowState = this.WindowState;
                _previousWidth = this.Width;
                _previousHeight = this.Height;
                _previousLeft = this.Left;
                _previousTop = this.Top;

                SystemCommands.RestoreWindow(this);
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.NoResize;
                this.Topmost = true;

                _windowResizer.IsFullScreen = true;
            }
            else
            {
                _isFullScreen = false;
                _windowResizer.IsFullScreen = false;
                
                this.Topmost = false;
                this.WindowStyle = WindowStyle.None;
                this.ResizeMode = ResizeMode.CanResize;

                if (_previousWindowState == WindowState.Maximized)
                {
                    SystemCommands.MaximizeWindow(this);
                }
                else
                {
                    SystemCommands.RestoreWindow(this);
                    this.Width = _previousWidth;
                    this.Height = _previousHeight;
                    this.Left = _previousLeft;
                    this.Top = _previousTop;
                }
            }
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                if (this.WindowState == WindowState.Maximized)
                {

                    var mousePos = e.GetPosition(this);
                    double widthRatio = mousePos.X / this.ActualWidth;
                    
                    SystemCommands.RestoreWindow(this);

                    var screenMousePos = this.PointToScreen(mousePos);
                    this.Left = screenMousePos.X - (this.ActualWidth * widthRatio);
                    this.Top = screenMousePos.Y - mousePos.Y;
                }
                
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    this.DragMove();
                }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullScreen) ToggleFullScreen();
            SystemCommands.MinimizeWindow(this);
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isFullScreen) ToggleFullScreen();

            if (this.WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }
    }
}
