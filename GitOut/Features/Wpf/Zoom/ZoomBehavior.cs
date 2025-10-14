using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using GitOut.Features.Input;

namespace GitOut.Features.Wpf.Zoom
{
    public static class ZoomBehavior
    {
        private const double ScaleStep = .2;

        public static readonly DependencyProperty ZoomInKeyGestureProperty =
            DependencyProperty.RegisterAttached(
                "ZoomInKeyGesture",
                typeof(KeyGesture),
                typeof(ZoomBehavior),
                new PropertyMetadata(OnZoomInKeyGestureChanged)
            );

        public static readonly DependencyProperty ZoomOutKeyGestureProperty =
            DependencyProperty.RegisterAttached(
                "ZoomOutKeyGesture",
                typeof(KeyGesture),
                typeof(ZoomBehavior),
                new PropertyMetadata(OnZoomOutKeyGestureChanged)
            );

        public static readonly DependencyProperty ZoomResetKeyGestureProperty =
            DependencyProperty.RegisterAttached(
                "ZoomResetKeyGesture",
                typeof(KeyGesture),
                typeof(ZoomBehavior),
                new PropertyMetadata(OnResetZoomKeyGestureChanged)
            );

        public static readonly DependencyProperty ZoomInMouseWheelGestureProperty =
            DependencyProperty.RegisterAttached(
                "ZoomInMouseWheelGesture",
                typeof(MouseWheelGesture),
                typeof(ZoomBehavior),
                new PropertyMetadata(OnZoomInMouseWheelGestureChanged)
            );

        public static readonly DependencyProperty ZoomOutMouseWheelGestureProperty =
            DependencyProperty.RegisterAttached(
                "ZoomOutMouseWheelGesture",
                typeof(MouseWheelGesture),
                typeof(ZoomBehavior),
                new PropertyMetadata(OnZoomOutMouseWheelGestureChanged)
            );

        public static KeyGesture? GetZoomInKeyGesture(DependencyObject obj) =>
            (KeyGesture?)obj.GetValue(ZoomInKeyGestureProperty);

        public static KeyGesture? GetZoomOutKeyGesture(DependencyObject obj) =>
            (KeyGesture?)obj.GetValue(ZoomOutKeyGestureProperty);

        public static KeyGesture? GetZoomResetKeyGesture(DependencyObject obj) =>
            (KeyGesture?)obj.GetValue(ZoomResetKeyGestureProperty);

        public static MouseWheelGesture? GetZoomInMouseWheelGesture(DependencyObject obj) =>
            (MouseWheelGesture?)obj.GetValue(ZoomInMouseWheelGestureProperty);

        public static MouseWheelGesture? GetZoomOutMouseWheelGesture(DependencyObject obj) =>
            (MouseWheelGesture?)obj.GetValue(ZoomOutMouseWheelGestureProperty);

        public static void SetZoomInKeyGesture(DependencyObject obj, KeyGesture? value) =>
            obj.SetValue(ZoomInKeyGestureProperty, value);

        public static void SetZoomOutKeyGesture(DependencyObject obj, KeyGesture? value) =>
            obj.SetValue(ZoomOutKeyGestureProperty, value);

        public static void SetZoomResetKeyGesture(DependencyObject obj, KeyGesture? value) =>
            obj.SetValue(ZoomResetKeyGestureProperty, value);

        public static void SetZoomInMouseWheelGesture(
            DependencyObject obj,
            MouseWheelGesture? value
        ) => obj.SetValue(ZoomInMouseWheelGestureProperty, value);

        public static void SetZoomOutMouseWheelGesture(
            DependencyObject obj,
            MouseWheelGesture? value
        ) => obj.SetValue(ZoomOutMouseWheelGestureProperty, value);

        private static void OnZoomInKeyGestureChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is FrameworkElement element)
            {
                element.PreviewKeyDown -= OnZoomIn;
                element.PreviewKeyDown += OnZoomIn;
            }
        }

        private static void OnZoomOutKeyGestureChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is FrameworkElement element)
            {
                element.PreviewKeyDown -= OnZoomOut;
                element.PreviewKeyDown += OnZoomOut;
            }
        }

        private static void OnResetZoomKeyGestureChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is FrameworkElement element)
            {
                element.PreviewKeyDown -= OnResetZoom;
                element.PreviewKeyDown += OnResetZoom;
            }
        }

        private static void OnZoomInMouseWheelGestureChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is FrameworkElement element && e.NewValue is MouseWheelGesture mouseWheelGesture)
            {
                switch (mouseWheelGesture.Action)
                {
                    case MouseWheelAction.MouseWheelDown:
                    case MouseWheelAction.MouseWheelUp:
                        element.PreviewMouseWheel -= OnMouseWheelZoomIn;
                        element.PreviewMouseWheel += OnMouseWheelZoomIn;
                        return;
                }
            }
        }

        private static void OnZoomOutMouseWheelGestureChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is FrameworkElement element && e.NewValue is MouseWheelGesture mouseWheelGesture)
            {
                switch (mouseWheelGesture.Action)
                {
                    case MouseWheelAction.MouseWheelDown:
                    case MouseWheelAction.MouseWheelUp:
                        element.PreviewMouseWheel -= OnMouseWheelZoomOut;
                        element.PreviewMouseWheel += OnMouseWheelZoomOut;
                        return;
                }
            }
        }

        private static void OnZoomIn(object sender, KeyEventArgs e)
        {
            if (
                sender is FrameworkElement element
                && GetZoomInKeyGesture(element) is KeyGesture zoomIn
                && zoomIn.Key == e.Key
                && zoomIn.Modifiers == Keyboard.Modifiers
            )
            {
                ScaleTransform transform = GetElementScaleTransform(element);
                if (transform.ScaleX <= 3)
                {
                    transform.ScaleX += ScaleStep;
                    transform.ScaleY += ScaleStep;
                }
            }
        }

        private static void OnZoomOut(object sender, KeyEventArgs e)
        {
            if (
                sender is FrameworkElement element
                && GetZoomOutKeyGesture(element) is KeyGesture zoomOut
                && zoomOut.Key == e.Key
                && zoomOut.Modifiers == Keyboard.Modifiers
            )
            {
                ScaleTransform transform = GetElementScaleTransform(element);
                if (transform.ScaleX >= .5)
                {
                    transform.ScaleX -= ScaleStep;
                    transform.ScaleY -= ScaleStep;
                }
            }
        }

        private static void OnResetZoom(object sender, KeyEventArgs e)
        {
            if (
                sender is FrameworkElement element
                && GetZoomResetKeyGesture(element) is KeyGesture zoomReset
                && zoomReset.Key == e.Key
                && zoomReset.Modifiers == Keyboard.Modifiers
            )
            {
                ScaleTransform transform = GetElementScaleTransform(element);
                transform.ScaleX = 1;
                transform.ScaleY = 1;
            }
        }

        private static void OnMouseWheelZoomIn(object sender, MouseWheelEventArgs e)
        {
            if (
                sender is FrameworkElement element
                && GetZoomInMouseWheelGesture(element) is MouseWheelGesture zoomIn
                && zoomIn.Matches(e)
                && zoomIn.Modifiers == Keyboard.Modifiers
            )
            {
                ScaleTransform transform = GetElementScaleTransform(element);
                if (transform.ScaleX <= 3)
                {
                    transform.ScaleX += ScaleStep;
                    transform.ScaleY += ScaleStep;
                }
            }
        }

        private static void OnMouseWheelZoomOut(object sender, MouseWheelEventArgs e)
        {
            if (
                sender is FrameworkElement element
                && GetZoomOutMouseWheelGesture(element) is MouseWheelGesture zoomOut
                && zoomOut.Matches(e)
                && zoomOut.Modifiers == Keyboard.Modifiers
            )
            {
                ScaleTransform transform = GetElementScaleTransform(element);
                if (transform.ScaleX >= .5)
                {
                    transform.ScaleX -= ScaleStep;
                    transform.ScaleY -= ScaleStep;
                }
            }
        }

        private static ScaleTransform GetElementScaleTransform(FrameworkElement element) =>
            (element.LayoutTransform is ScaleTransform scaleTransform)
                ? scaleTransform
                : (ScaleTransform)(element.LayoutTransform = new ScaleTransform());
    }
}
