using System.Windows;
using System.Windows.Controls;

namespace GitOut.Features.ReleaseNotes
{
    public partial class MarkdownViewer : UserControl
    {
        public static readonly DependencyProperty MarkdownTextProperty = DependencyProperty.Register(
            nameof(MarkdownText),
            typeof(string),
            typeof(MarkdownViewer),
            new PropertyMetadata("")
        );

        public MarkdownViewer() => InitializeComponent();

        public string MarkdownText
        {
            get => (string)GetValue(MarkdownTextProperty);
            set => SetValue(MarkdownTextProperty, value);
        }
    }
}
