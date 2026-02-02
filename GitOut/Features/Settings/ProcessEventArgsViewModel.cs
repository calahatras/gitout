using System;
using System.Windows.Input;
using GitOut.Features.Diagnostics;
using GitOut.Features.Material.Snackbar;
using GitOut.Features.Wpf;

namespace GitOut.Features.Settings;

public class ProcessEventArgsViewModel
{
    public ProcessEventArgsViewModel(ProcessEventArgs model, ISnackbarService snacks)
    {
        Input = model.Input;
        Output = model.Output;
        Error = model.Error;
        ProcessName = model.ProcessName;
        Arguments = model.Options.Arguments;
        WorkingDirectory = model.WorkingDirectory.Directory;
        StartTime = model.StartTime;
        Duration = model.Duration;
        CopyCommand = new CopyTextToClipBoardCommand<object>(
            o => $"{model.ProcessName} {model.Options.Arguments}",
            o => true,
            t => snacks.ShowSuccess("Copied command to clipboard")
        );
    }

    public ICommand CopyCommand { get; }
    public string Input { get; }
    public string Output { get; }
    public string Error { get; }
    public string ProcessName { get; }
    public string Arguments { get; }
    public string WorkingDirectory { get; }
    public DateTimeOffset StartTime { get; }
    public TimeSpan Duration { get; }
}
