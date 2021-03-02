using System;
using System.Collections.Generic;
using System.Text;
using GitOut.Features.IO;

namespace GitOut.Features.Diagnostics
{
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
            Input = input;
            Output = output;
            Error = error;
        }

        public string ProcessName { get; }
        public DirectoryPath WorkingDirectory { get; }
        public ProcessOptions Options { get; }

        public DateTimeOffset StartTime { get; }
        public TimeSpan Duration { get; }
        public StringBuilder Input { get; }
        public IReadOnlyCollection<string> Output { get; }
        public IReadOnlyCollection<string> Error { get; }
    }
}
