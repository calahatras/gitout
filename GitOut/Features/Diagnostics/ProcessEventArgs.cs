using System;
using System.Collections.Generic;
using System.Text;
using GitOut.Features.IO;

namespace GitOut.Features.Diagnostics;

public class ProcessEventArgs
{
    public ProcessEventArgs(
        string processName,
        DirectoryPath workingDirectory,
        ProcessOptions options,
        DateTimeOffset startTime,
        TimeSpan duration,
        StringBuilder input,
        IReadOnlyCollection<string> output,
        IReadOnlyCollection<string> error
    )
    {
        ProcessName = processName;
        WorkingDirectory = workingDirectory;
        Options = options;
        StartTime = startTime;
        Duration = duration;
        Input = input.ToString();
        Output = string.Join(Environment.NewLine, output);
        OutputLines = output;
        Error = string.Join(Environment.NewLine, error);
        ErrorLines = error;
    }

    public string ProcessName { get; }
    public DirectoryPath WorkingDirectory { get; }
    public ProcessOptions Options { get; }

    public DateTimeOffset StartTime { get; }
    public TimeSpan Duration { get; }

    public string Input { get; }
    public string Output { get; }
    public string Error { get; }

    public IReadOnlyCollection<string> OutputLines { get; }
    public IReadOnlyCollection<string> ErrorLines { get; }
}
