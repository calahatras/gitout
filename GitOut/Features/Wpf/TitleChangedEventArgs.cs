namespace GitOut.Features.Wpf
{
    public class TitleChangedEventArgs
    {
        public TitleChangedEventArgs(string? title) => Title = title;

        public string? Title { get; }
    }
}
