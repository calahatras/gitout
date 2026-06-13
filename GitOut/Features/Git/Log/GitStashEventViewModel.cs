using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.Log;

public sealed class GitStashEventViewModel : INotifyPropertyChanged
{
    public GitStashEventViewModel(GitStash stashEvent, ICommand createBranchCommand)
    {
        Event = stashEvent;
        CreateBranchCommand = createBranchCommand;
        CopyStashNameCommand = new CopyTextToClipBoardCommand<GitStashEventViewModel>(vm =>
            $"stash@{{{vm?.StashIndex ?? 0}}}"
        );
        CopyStashIdCommand = new CopyTextToClipBoardCommand<GitStashEventViewModel>(vm =>
            vm?.Event.Id.Hash ?? string.Empty
        );
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public GitStash Event { get; }
    public int StashIndex => Event.StashIndex;

    public bool IsSelected
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand CreateBranchCommand { get; }
    public ICommand CopyStashNameCommand { get; }
    public ICommand CopyStashIdCommand { get; }

    private void SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!ReferenceEquals(prop, value))
        {
            prop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
