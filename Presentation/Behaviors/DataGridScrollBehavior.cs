using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AlJohary.ServiceHub.Presentation.Behaviors
{
    public static class DataGridScrollBehavior
    {
        public static readonly DependencyProperty ForwardMouseWheelToParentProperty =
            DependencyProperty.RegisterAttached(
                "ForwardMouseWheelToParent",
                typeof(bool),
                typeof(DataGridScrollBehavior),
                new PropertyMetadata(false, OnForwardMouseWheelToParentChanged));

        public static bool GetForwardMouseWheelToParent(DependencyObject obj)
            => (bool)obj.GetValue(ForwardMouseWheelToParentProperty);

        public static void SetForwardMouseWheelToParent(DependencyObject obj, bool value)
            => obj.SetValue(ForwardMouseWheelToParentProperty, value);

        private static void OnForwardMouseWheelToParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DataGrid dataGrid && (bool)e.NewValue)
            {
                dataGrid.PreviewMouseWheel += OnPreviewMouseWheel;
            }
        }

        private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            var scrollViewer = FindDescendantScrollViewer(dataGrid);
            if (scrollViewer == null) return;

            bool atTop = scrollViewer.VerticalOffset <= 0;
            bool atBottom = scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight;
            bool scrollingUp = e.Delta > 0;
            bool scrollingDown = e.Delta < 0;

            if ((scrollingUp && atTop) || (scrollingDown && atBottom))
            {
                var parentScrollViewer = FindAncestorScrollViewer(dataGrid);
                if (parentScrollViewer != null)
                {
                    e.Handled = true;
                    var args = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                    args.RoutedEvent = UIElement.MouseWheelEvent;
                    parentScrollViewer.RaiseEvent(args);
                }
            }
        }

        private static ScrollViewer FindDescendantScrollViewer(DependencyObject parent)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is ScrollViewer sv) return sv;
                var result = FindDescendantScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private static ScrollViewer FindAncestorScrollViewer(DependencyObject element)
        {
            var parent = VisualTreeHelper.GetParent(element);
            while (parent != null)
            {
                if (parent is ScrollViewer sv) return sv;
                parent = VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
