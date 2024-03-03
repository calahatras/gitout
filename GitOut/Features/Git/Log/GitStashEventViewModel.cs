using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace GitOut.Features.Git.Log;

public sealed class GitStashEventViewModel : INotifyPropertyChanged
{
    private bool isSelected;
    public GitStashEventViewModel(
        GitStash stashEvent,
        ICommand createBranchCommand
    )
    {
        Event = stashEvent;
        CreateBranchCommand = createBranchCommand;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public GitStash Event { get; }
    public int StashIndex => Event.StashIndex;

    public bool IsSelected
    {
        get => isSelected;
        set => SetProperty(ref isSelected, value);
    }

    public ICommand CreateBranchCommand { get; }

    private void SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!ReferenceEquals(prop, value))
        {
            prop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
