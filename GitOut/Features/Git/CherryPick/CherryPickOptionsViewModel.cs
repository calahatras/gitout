using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;

namespace GitOut.Features.Git.CherryPick;

public sealed class CherryPickOptionsViewModel : INotifyPropertyChanged
{
    public CherryPickOptionsViewModel(INavigationService navigation, ITitleService title)
    {
        title.Title = "Cherry pick options";
        CherryPickPrepareOptions? prepared = navigation.GetOptions<CherryPickPrepareOptions>(
            typeof(CherryPickOptionsPage).FullName!
        );
        CancelCommand = new CallbackCommand(navigation.Close);
        SetResultCommand = new CallbackCommand(() =>
            navigation.Close(
                new GitCherryPickOptions(
                    Edit: Edit,
                    NoCommit: NoCommit,
                    MainlineParentNumber: MainlineParentNumber > 0 ? MainlineParentNumber : null,
                    AppendCherryPickLine: AppendCherryPickLine,
                    FastForward: FastForward
                )
            )
        );
    }

    public bool Edit
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool NoCommit
    {
        get;
        set => SetProperty(ref field, value);
    }

    public int MainlineParentNumber
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool AppendCherryPickLine
    {
        get;
        set => SetProperty(ref field, value);
    }

    public bool FastForward
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICommand CancelCommand { get; }
    public ICommand SetResultCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetProperty<T>(ref T prop, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!ReferenceEquals(prop, value))
        {
            prop = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
