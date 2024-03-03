using System.Windows;

namespace GitOut.Features.Navigation;

public sealed record NavigationOverrideOptions(
    Size WindowSize,
    Point Offset,
    bool IsModal = false,
    bool IsStatusBarVisible = true
);
