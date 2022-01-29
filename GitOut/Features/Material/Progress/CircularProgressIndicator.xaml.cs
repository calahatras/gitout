using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GitOut.Features.Material.Progress
{
    public partial class CircularProgressIndicator : UserControl
    {
        public static readonly DependencyProperty DiameterProperty = DependencyProperty.Register(
            nameof(Diameter),
            typeof(double),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(32d, FrameworkPropertyMetadataOptions.AffectsMeasure, OnDiameterChanged)
        );

        public static readonly DependencyProperty StrokeProperty = DependencyProperty.Register(
            nameof(Stroke),
            typeof(Brush),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
            nameof(StrokeThickness),
            typeof(double),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(3d, FrameworkPropertyMetadataOptions.None, OnStrokeThicknessChanged)
        );

        public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
            nameof(Scale),
            typeof(double),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsParentMeasure)
        );

        public static readonly DependencyProperty ScaledStrokeThicknessProperty = DependencyProperty.Register(
            nameof(ScaledStrokeThickness),
            typeof(double),
            typeof(CircularProgressIndicator),
            new FrameworkPropertyMetadata(3d, FrameworkPropertyMetadataOptions.AffectsRender)
        );

        public CircularProgressIndicator() => this.InitializeComponent();

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

        private static void OnDiameterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressIndicator circle)
            {
                double diameter = (double)e.NewValue;
                double thickness = circle.StrokeThickness;
                circle.Scale = diameter / 100;
                circle.ScaledStrokeThickness = thickness / circle.Scale;
            }
        }

        private static void OnStrokeThicknessChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is CircularProgressIndicator circle)
            {
                double thickness = (double)e.NewValue;
                circle.ScaledStrokeThickness = thickness / circle.Scale;
            }
        }
    }
}
