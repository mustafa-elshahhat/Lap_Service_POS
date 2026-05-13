using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CarPartsShopWPF.Presentation.Behaviors
{
    public static class TextBoxBehavior
    {
        public static readonly DependencyProperty SelectAllOnFocusProperty =
            DependencyProperty.RegisterAttached(
                "SelectAllOnFocus",
                typeof(bool),
                typeof(TextBoxBehavior),
                new PropertyMetadata(false, OnSelectAllOnFocusChanged));

        public static bool GetSelectAllOnFocus(DependencyObject obj)
        {
            return (bool)obj.GetValue(SelectAllOnFocusProperty);
        }

        public static void SetSelectAllOnFocus(DependencyObject obj, bool value)
        {
            obj.SetValue(SelectAllOnFocusProperty, value);
        }

        private static void OnSelectAllOnFocusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox)
            {
                if ((bool)e.NewValue)
                {
                    textBox.GotKeyboardFocus += OnTextBoxGotKeyboardFocus;
                    textBox.PreviewMouseLeftButtonDown += OnTextBoxPreviewMouseLeftButtonDown;
                }
                else
                {
                    textBox.GotKeyboardFocus -= OnTextBoxGotKeyboardFocus;
                    textBox.PreviewMouseLeftButtonDown -= OnTextBoxPreviewMouseLeftButtonDown;
                }
            }
        }

        private static void OnTextBoxGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private static void OnTextBoxPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();

            }
        }
    }
}
