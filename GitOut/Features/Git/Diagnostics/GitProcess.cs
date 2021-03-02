using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitOut.Features.Diagnostics;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diagnostics
{
    public class GitProcess : IGitProcess
    {
        private const string CommandLineExecutable = "git";
        private readonly DirectoryPath workingDirectory;
        private readonly ProcessOptions arguments;
        private readonly IProcessTelemetryCollector telemetry;

        public GitProcess(
            DirectoryPath workingDirectory,
            ProcessOptions arguments,
            IProcessTelemetryCollector telemetry
        )
        {
            this.workingDirectory = workingDirectory;
            this.arguments = arguments;
            this.telemetry = telemetry;
        }

        public Task<ProcessEventArgs> ExecuteAsync(CancellationToken cancellationToken = default) => ExecuteAsync(new StringBuilder(), cancellationToken);

        public async Task<ProcessEventArgs> ExecuteAsync(StringBuilder writer, CancellationToken cancellationToken = default)
        {
            using var exec = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = CommandLineExecutable,
                    Arguments = arguments.Arguments,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory.Directory
                }
            };
            exec.Start();

            var source = new TaskCompletionSource<bool>();
            var output = new List<string>();
            var error = new List<string>();
            exec.OutputDataReceived += OnHandleOutputData;
            exec.ErrorDataReceived += OnHandleErrorData;
            exec.Exited += (sender, e) => source.SetResult(string.IsNullOrEmpty(error.ToString()));

            exec.BeginErrorReadLine();
            exec.BeginOutputReadLine();

            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(source.SetCanceled);
            }
            Trace.WriteLine("Writing to stream:");
            Trace.WriteLine(writer.ToString());
            Trace.WriteLine("======");
            using (StreamWriter processInput = exec.StandardInput)
            {
                await processInput.WriteAsync(writer, cancellationToken);
            }
            bool isSuccessful = await source.Task;

            TimeSpan duration = exec.ExitTime - exec.StartTime;
            Trace.WriteLine($"Running command {arguments.Arguments}: {duration.TotalMilliseconds}ms");
            var args = new ProcessEventArgs(CommandLineExecutable, workingDirectory, arguments, new DateTimeOffset(exec.StartTime), duration, writer, output.AsReadOnly(), error.AsReadOnly());
            telemetry.Report(args);
            if (!isSuccessful)
            {
                foreach (string line in error)
                {
                    Trace.WriteLine(line);
                }
            }
            return args;

            void OnHandleOutputData(object sender, DataReceivedEventArgs e)
            {
                string? data = e.Data;
                if (!(data is null))
                {
                    output.Add(data);
                    Trace.WriteLine($"{data}");
                }
            }
            void OnHandleErrorData(object sender, DataReceivedEventArgs e)
            {
                string? data = e.Data;
                if (!(data is null))
                {
                    error.Add(data);
                    Trace.WriteLine($"{data}");
                }
            }
        }

        public async IAsyncEnumerable<string> ReadLinesAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var dataCounter = new CountdownEvent(3);
            var dataReceivedEvent = new ManualResetEventSlim(false);
            using var exec = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = CommandLineExecutable,
                    Arguments = arguments.Arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory.Directory
                }
            };
            var output = new List<string>();
            var error = new List<string>();
            var queue = new BufferBlock<string>();
            exec.OutputDataReceived += OnHandleOutputData;
            exec.ErrorDataReceived += OnHandleErrorData;
            exec.Exited += (sender, e) => dataCounter.Signal();
            exec.Start();

            exec.BeginErrorReadLine();
            exec.BeginOutputReadLine();

            dataReceivedEvent.Wait(cancellationToken);
            while (!dataCounter.IsSet || queue.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (queue.TryReceive(out string item))
                {
                    yield return item;
                }
                else
                {
                    await Task.Yield();
                }
            }
            queue.Complete();
            TimeSpan duration = exec.ExitTime - exec.StartTime;
            Trace.WriteLine($"Running command {arguments.Arguments}: {duration.TotalMilliseconds}ms");
            telemetry.Report(new ProcessEventArgs(CommandLineExecutable, workingDirectory, arguments, new DateTimeOffset(exec.StartTime), duration, new StringBuilder(), output.AsReadOnly(), error.AsReadOnly()));

            void OnHandleOutputData(object sender, DataReceivedEventArgs e)
            {
                string? data = e.Data;
                if (data is null)
                {
                    dataCounter.Signal();
                }
                else
                {
                    output.Add(data);
                    queue.Post(data);
                }
                dataReceivedEvent.Set();
            }
            void OnHandleErrorData(object sender, DataReceivedEventArgs e)
            {
                string? data = e.Data;
                if (data is null)
                {
                    dataCounter.Signal();
                }
                else
                {
                    error.Add(data);
                }
                dataReceivedEvent.Set();
            }
        }
    }
}
