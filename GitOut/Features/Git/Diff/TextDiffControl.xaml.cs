using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace GitOut.Features.Git.Diff
{
    public partial class TextDiffControl : UserControl
    {
        public static readonly DependencyProperty DocumentProperty = DependencyProperty.Register(
            nameof(Document),
            typeof(FlowDocument),
            typeof(TextDiffControl),
            new PropertyMetadata(OnDocumentUpdated)
        );

        public static readonly DependencyProperty LineNumbersProperty = DependencyProperty.Register(
            nameof(LineNumbers),
            typeof(IEnumerable<LineNumberViewModel>),
            typeof(TextDiffControl),
            new PropertyMetadata(null)
        );

        public TextDiffControl() => InitializeComponent();

        public FlowDocument? Document
        {
            get => (FlowDocument?)GetValue(DocumentProperty);
            set => SetValue(DocumentProperty, value);
        }

        public IEnumerable<LineNumberViewModel>? LineNumbers
        {
            get => (IEnumerable<LineNumberViewModel>?)GetValue(LineNumbersProperty);
            set => SetValue(LineNumbersProperty, value);
        }

        private void TunnelEventToParentScroll(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Shift)
            {
                DocumentScroll.ScrollToHorizontalOffset(DocumentScroll.HorizontalOffset - e.Delta);
            }
            else
            {
                DocumentScroll.ScrollToVerticalOffset(DocumentScroll.VerticalOffset - e.Delta);
            }
        }

        private void CopySelectedText(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            Clipboard.SetText(HunksViewer.Selection.Text.Replace('\u00B7', ' '), TextDataFormat.UnicodeText);
        }

        private static void OnDocumentUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextDiffControl textControl)
            {
                SynchronizationContext.Current?.Post(d =>
                {
                    var control = (TextDiffControl)d!;
                    if (control.DataContext is IDocumentSelectionViewModel vm && control.HunksViewer.Document == e.NewValue)
                    {
                        vm.Selection = control.HunksViewer.Selection;
                    }
                }, textControl);
            }
        }
    }
}
