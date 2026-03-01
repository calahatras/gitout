using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using GitOut.Features.Navigation;
using GitOut.Features.Wpf;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Git.Log;

public sealed class CherryPickOptionsViewModel : INotifyPropertyChanged
{
    private bool edit;
    private bool noCommit;
    private int mainlineParentNumber;
    private bool appendCherryPickLine;
    private bool fastForward;

    public CherryPickOptionsViewModel(
        INavigationService navigation,
        ITitleService title,
        IOptionsSnapshot<GitCherryPickOptions> options
    )
    {
        title.Title = "Cherry pick options";
        GitCherryPickOptions current = options.Value;
        edit = current.Edit;
        noCommit = current.NoCommit;
        mainlineParentNumber = current.MainlineParentNumber ?? 0;
        appendCherryPickLine = current.AppendCherryPickLine;
        fastForward = current.FastForward;

        CancelCommand = new CallbackCommand(navigation.Close);
        SetResultCommand = new CallbackCommand(() =>
            navigation.Close(
                new GitCherryPickOptions
                {
                    Edit = edit,
                    NoCommit = noCommit,
                    MainlineParentNumber = mainlineParentNumber > 0 ? mainlineParentNumber : null,
                    AppendCherryPickLine = appendCherryPickLine,
                    FastForward = fastForward,
                }
            )
        );
    }

    public bool Edit
    {
        get => edit;
        set => SetProperty(ref edit, value);
    }

    public bool NoCommit
    {
        get => noCommit;
        set => SetProperty(ref noCommit, value);
    }

    public int MainlineParentNumber
    {
        get => mainlineParentNumber;
        set => SetProperty(ref mainlineParentNumber, value);
    }

    public bool AppendCherryPickLine
    {
        get => appendCherryPickLine;
        set => SetProperty(ref appendCherryPickLine, value);
    }

    public bool FastForward
    {
        get => fastForward;
        set => SetProperty(ref fastForward, value);
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
