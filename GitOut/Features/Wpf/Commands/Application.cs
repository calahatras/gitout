namespace GitOut.Features.Wpf.Commands
{
    public static class Application
    {
        public static CompositeCommand<string> Open { get; } = new CompositeCommand<string>();
        public static CompositeCommand<string> Copy { get; } = new CompositeCommand<string>();
        public static CompositeCommand<string> RevealInExplorer { get; } = new CompositeCommand<string>();
    }
}
