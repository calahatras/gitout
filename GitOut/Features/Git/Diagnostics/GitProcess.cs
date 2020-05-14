using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diagnostics
{
    public class GitProcess : IGitProcess
    {
        private readonly DirectoryPath workingDirectory;
        private readonly GitProcessOptions arguments;

        public GitProcess(DirectoryPath workingDirectory, GitProcessOptions arguments)
        {
            this.workingDirectory = workingDirectory;
            this.arguments = arguments;
        }

        public async Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            using var exec = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments.Arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory.Directory
                }
            };
            var watch = Stopwatch.StartNew();
            exec.Start();
            var source = new TaskCompletionSource<bool>();
            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(source.SetCanceled);
            }
            exec.Exited += (sender, e) => source.SetResult(true);
            await source.Task;
            Trace.WriteLine($"Running command {arguments.Arguments}: {watch.Elapsed.TotalMilliseconds}ms");
        }

        public async Task ExecuteAsync(StringBuilder writer, CancellationToken cancellationToken = default)
        {
            using var exec = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments.Arguments,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory.Directory
                }
            };
            var watch = Stopwatch.StartNew();
            exec.Start();
            var source = new TaskCompletionSource<bool>();
            if (cancellationToken != CancellationToken.None)
            {
                cancellationToken.Register(source.SetCanceled);
            }
            exec.Exited += (sender, e) => source.SetResult(true);
            using (StreamWriter processInput = exec.StandardInput)
            {
                await processInput.WriteAsync(writer, cancellationToken);
            }
            await source.Task;
            Trace.WriteLine($"Running command {arguments.Arguments}: {watch.Elapsed.TotalMilliseconds}ms");
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
                    FileName = "git",
                    Arguments = arguments.Arguments,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory.Directory
                }
            };
            var queue = new BufferBlock<string>();
            exec.OutputDataReceived += OnHandleData;
            exec.ErrorDataReceived += OnHandleData;
            exec.Exited += (sender, e) => dataCounter.Signal();
            var watch = Stopwatch.StartNew();
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
            Trace.WriteLine($"Running command {arguments.Arguments}: {watch.Elapsed.TotalMilliseconds}ms");

            void OnHandleData(object sender, DataReceivedEventArgs e)
            {
                string? data = e.Data;
                dataReceivedEvent.Set();
                if (data == null)
                {
                    dataCounter.Signal();
                }
                else
                {
                    queue.Post(data);
                }
            }
        }
    }
}
