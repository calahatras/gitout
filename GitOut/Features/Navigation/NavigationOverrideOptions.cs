using System.Windows;

namespace GitOut.Features.Navigation;

public record NavigationOverrideOptions(
    Size WindowSize,
    Point Offset,
    bool IsModal = false,
    bool IsStatusBarVisible = true
);
