namespace GitOut.Features.Navigation
{
    public interface INavigationFallback
    {
        string FallbackPageName { get; }
        object? FallbackOptions { get; }
    }
}
