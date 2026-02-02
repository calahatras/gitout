using System.Windows;
using System.Windows.Input;

namespace GitOut.Features.Git.Details;

public partial class GitHistoryEventDetails
{
    public static readonly DependencyProperty GitHistoryEventProperty = DependencyProperty.Register(
        nameof(GitHistoryEvent),
        typeof(GitHistoryEvent),
        typeof(GitHistoryEventDetails)
    );

    public static readonly DependencyProperty CopyHashCommandProperty = DependencyProperty.Register(
        nameof(CopyHashCommand),
        typeof(ICommand),
        typeof(GitHistoryEventDetails)
    );

    public static readonly DependencyProperty SelectCommitCommandProperty =
        DependencyProperty.Register(
            nameof(SelectCommitCommand),
            typeof(ICommand),
            typeof(GitHistoryEventDetails)
        );

    public static readonly DependencyProperty AppendSelectCommitCommandProperty =
        DependencyProperty.Register(
            nameof(AppendSelectCommitCommand),
            typeof(ICommand),
            typeof(GitHistoryEventDetails)
        );

    public GitHistoryEventDetails() => InitializeComponent();

    public GitHistoryEvent GitHistoryEvent
    {
        get => (GitHistoryEvent)GetValue(GitHistoryEventProperty);
        set => SetValue(GitHistoryEventProperty, value);
    }

    public ICommand CopyHashCommand
    {
        get => (ICommand)GetValue(CopyHashCommandProperty);
        set => SetValue(CopyHashCommandProperty, value);
    }

    public ICommand SelectCommitCommand
    {
        get => (ICommand)GetValue(SelectCommitCommandProperty);
        set => SetValue(SelectCommitCommandProperty, value);
    }

    public ICommand AppendSelectCommitCommand
    {
        get => (ICommand)GetValue(AppendSelectCommitCommandProperty);
        set => SetValue(AppendSelectCommitCommandProperty, value);
    }
}
