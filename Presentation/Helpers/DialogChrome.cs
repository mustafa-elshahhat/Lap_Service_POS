using System.Collections.Generic;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace AlJohary.ServiceHub.Presentation.Helpers
{
    public static class DialogChrome
    {
        public static readonly DependencyProperty DimOwnerProperty =
            DependencyProperty.RegisterAttached(
                "DimOwner", typeof(bool), typeof(DialogChrome),
                new PropertyMetadata(false, OnDimOwnerChanged));

        public static void SetDimOwner(DependencyObject d, bool value) =>
            d.SetValue(DimOwnerProperty, value);
        public static bool GetDimOwner(DependencyObject d) =>
            (bool)d.GetValue(DimOwnerProperty);

        private sealed class OwnerState
        {
            public UIElement Content;
            public DimAdorner Adorner;
            public AdornerLayer Layer;
            public Effect PreviousEffect;
            public int Count;
        }

        private static readonly Dictionary<Window, OwnerState> _states =
            new Dictionary<Window, OwnerState>();

        private static void OnDimOwnerChanged(
            DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window w && e.NewValue is bool b && b)
            {
                w.Loaded += Window_Loaded;
                w.Closed += Window_Closed;
            }
        }

        private static void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var w = (Window)sender;
            var owner = w.Owner;
            if (owner == null) return;
            if (!(owner.Content is UIElement content)) return;

            var layer = AdornerLayer.GetAdornerLayer(content);
            if (layer == null) return;

            if (!_states.TryGetValue(owner, out var state))
            {
                state = new OwnerState
                {
                    Content = content,
                    Layer = layer,
                    PreviousEffect = content.Effect,
                    Adorner = new DimAdorner(content)
                };
                layer.Add(state.Adorner);
                content.Effect = new BlurEffect { Radius = 8, KernelType = KernelType.Gaussian };
                _states[owner] = state;
            }
            state.Count++;
        }

        private static void Window_Closed(object sender, System.EventArgs e)
        {
            var w = (Window)sender;
            var owner = w.Owner;
            if (owner == null) return;
            if (!_states.TryGetValue(owner, out var state)) return;

            state.Count--;
            if (state.Count <= 0)
            {
                state.Layer?.Remove(state.Adorner);
                if (state.Content != null) state.Content.Effect = state.PreviousEffect;
                _states.Remove(owner);
            }
        }

        private sealed class DimAdorner : Adorner
        {
            private static readonly Brush _brush =
                new SolidColorBrush(Color.FromArgb(0x80, 0x00, 0x00, 0x00));

            static DimAdorner()
            {
                if (_brush.CanFreeze) _brush.Freeze();
            }

            public DimAdorner(UIElement adornedElement) : base(adornedElement)
            {
                IsHitTestVisible = true;
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                drawingContext.DrawRectangle(_brush, null,
                    new Rect(AdornedElement.RenderSize));
            }
        }
    }
}
