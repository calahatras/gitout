using System;
using System.Globalization;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GitOut.Features.Wpf.DragDrop
{
    public class DropAdorner : Adorner
    {
        private readonly Func<IDataObject, string> textSelector;

        private bool isDragging;
        private string textToRender = string.Empty;

        public DropAdorner(UIElement adornedElement, Func<IDataObject, string> textSelector)
            : base(adornedElement)
        {
            IsHitTestVisible = false;
            adornedElement.DragEnter += OnAdornedElementDragEnter;
            adornedElement.DragLeave += OnAdornedElementDragLeave;
            adornedElement.Drop += OnAdornedElementDrop;
            this.textSelector = textSelector;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!isDragging)
            {
                return;
            }

            Rect adornedElementRect = new(AdornedElement.RenderSize);
            Brush? brush = DragDropBehavior.GetAdornerStrokeBrush(AdornedElement);
            Pen renderPen = new(brush, 3) { DashStyle = new DashStyle(new[] { 4d, 4 }, 0) };

            drawingContext.DrawRectangle(Brushes.Transparent, renderPen, adornedElementRect);

            var textblock = new FormattedText(
                textToRender,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface(
                    new FontFamily("Roboto"),
                    FontStyles.Italic,
                    FontWeights.Normal,
                    FontStretches.Normal
                ),
                48,
                brush,
                1
            );

            var textLocation = new Point(
                adornedElementRect.Width / 2 - textblock.WidthIncludingTrailingWhitespace / 2,
                adornedElementRect.Height / 2 - textblock.Height
            );
            drawingContext.DrawText(textblock, textLocation);
        }

        private void OnAdornedElementDrop(object sender, DragEventArgs e)
        {
            isDragging = false;
            InvalidateVisual();
        }

        private void OnAdornedElementDragLeave(object sender, DragEventArgs e)
        {
            isDragging = false;
            InvalidateVisual();
        }

        private void OnAdornedElementDragEnter(object sender, DragEventArgs e)
        {
            if (isDragging)
            {
                return;
            }

            isDragging = true;
            textToRender = textSelector(e.Data);

            base.OnDragEnter(e);
            InvalidateVisual();
        }
    }
}
