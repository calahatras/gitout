using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GitOut.Features.Material.Progress
{
    public partial class CircularProgressIndicator : UserControl
    {
        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
            nameof(Diameter),
            typeof(double),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(
                32d,
                FrameworkPropertyMetadataOptions.AffectsMeasure,
                OnDiameterChanged
            )
        );

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(
                Brushes.White,
                FrameworkPropertyMetadataOptions.AffectsRender
            )
        );

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(StrokeThickness),
                typeof(double),
                typeof(CircularProgressIndicator),
                new FrameworkPropertyMetadata(
                    3d,
                    FrameworkPropertyMetadataOptions.None,
                    OnStrokeThicknessChanged
                )
            );

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
        );

        public static readonly DependencyProperty ScaledStrokeThicknessProperty =
            DependencyProperty.Register(
                nameof(ScaledStrokeThickness),
                typeof(double),
                typeof(CircularProgressIndicator),
                new FrameworkPropertyMetadata(3d, FrameworkPropertyMetadataOptions.AffectsRender)
            );

        public static readonly DependencyProperty ShowProgressProperty =
            DependencyProperty.Register(
                nameof(ShowProgress),
                typeof(bool),
                typeof(CircularProgressIndicator),
                new PropertyMetadata(false, OnShowProgressChanged)
            );

        private readonly ThrottleTimer<bool> debounceShowProgress;

        public CircularProgressIndicator()
        {
            InitializeComponent();
            debounceShowProgress = new ThrottleTimer<bool>(
                TimeSpan.FromSeconds(.8),
                value =>
                {
                    if (value)
                    {
                        CreateEnterAnimation();
                    }
                },
                value =>
                {
                    if (value)
                    {
                        CreateEnterAnimation();
                    }
                    else
                    {
                        CreateExitAnimation();
                    }
                }
            );
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            VisualStateManager.GoToElementState(this, "Normal", true);
        }

        public double Diameter
        {
            get => (double)GetValue(DiameterProperty);
            set => SetValue(DiameterProperty, value);
        }

        public Brush Stroke
        {
            get => (Brush)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        public double Scale
        {
            get => (double)GetValue(ScaleProperty);
            set => SetValue(ScaleProperty, value);
        }

        public double ScaledStrokeThickness
        {
            get => (double)GetValue(ScaledStrokeThicknessProperty);
            set => SetValue(ScaledStrokeThicknessProperty, value);
        }

        public bool ShowProgress
        {
            get => (bool)GetValue(ShowProgressProperty);
            set => SetValue(ShowProgressProperty, value);
        }

        private void CreateEnterAnimation()
        {
            var opacityAnimation = new DoubleAnimation(
                1,
                new Duration(TimeSpan.FromMilliseconds(200))
            )
            {
                EasingFunction = new ExponentialEase(),
                BeginTime = TimeSpan.FromMilliseconds(150),
            };
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            var widthAnimation = new DoubleAnimation(
                Diameter,
                new Duration(TimeSpan.FromMilliseconds(200))
            )
            {
                EasingFunction = new ExponentialEase(),
            };
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(WidthProperty));
            var story = new Storyboard();
            story.Children.Add(opacityAnimation);
            story.Children.Add(widthAnimation);
            BeginStoryboard(story);
        }

        private void CreateExitAnimation()
        {
            var opacityTiming = TimeSpan.FromMilliseconds(300);
            var widthTiming = TimeSpan.FromMilliseconds(200);
            var opacityAnimation = new DoubleAnimation(0, new Duration(opacityTiming))
            {
                EasingFunction = new ExponentialEase(),
            };
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath(OpacityProperty));
            var widthAnimation = new DoubleAnimation(0, new Duration(widthTiming))
            {
                EasingFunction = new ExponentialEase(),
                BeginTime = opacityTiming,
            };
            Storyboard.SetTargetProperty(widthAnimation, new PropertyPath(WidthProperty));
            var story = new Storyboard();
            story.Children.Add(opacityAnimation);
            story.Children.Add(widthAnimation);
            BeginStoryboard(story);
        }

        private static void OnDiameterChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is CircularProgressIndicator circle)
            {
                double diameter = (double)e.NewValue;
                double thickness = circle.StrokeThickness;
                circle.Scale = diameter / 100;
                circle.ScaledStrokeThickness = thickness / circle.Scale;
            }
        }

        private static void OnStrokeThicknessChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is CircularProgressIndicator circle)
            {
                double thickness = (double)e.NewValue;
                circle.ScaledStrokeThickness = thickness / circle.Scale;
            }
        }

        private static void OnShowProgressChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (d is CircularProgressIndicator circle && e.NewValue is bool value)
            {
                circle.debounceShowProgress.Update(value);
            }
        }

        private class ThrottleTimer<T>
        {
            private readonly Action<T> init;
            private readonly Action<T> update;
            private readonly DispatcherTimer t;

            private T? lastValue;

            public ThrottleTimer(TimeSpan throttleTime, Action<T> init, Action<T> update)
            {
                this.init = init;
                this.update = update;
                t = new DispatcherTimer(
                    throttleTime,
                    DispatcherPriority.Normal,
                    OnThrottled,
                    Application.Current.Dispatcher
                );
            }

            public void Update(T value)
            {
                lastValue = value;
                if (!t.IsEnabled)
                {
                    OnThrottleInitiated(lastValue);
                }
                else
                {
                    t.Stop();
                }
                t.Start();
            }

            private void OnThrottleInitiated(T value) => init(value);

            private void OnThrottled(object? sender, EventArgs e)
            {
                if (lastValue is not null)
                {
                    update(lastValue);
                }
                t.Stop();
            }
        }
    }
}
