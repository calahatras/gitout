using GitOut.Features.Diagnostics;
using GitOut.Features.IO;

namespace GitOut.Features.Git.Diagnostics;

public class GitProcessFactory : IProcessFactory<IGitProcess>
{
    private readonly IProcessTelemetryCollector telemetry;

    public GitProcessFactory(IProcessTelemetryCollector telemetry) => this.telemetry = telemetry;

    public IGitProcess Create(DirectoryPath workingDirectory, ProcessOptions arguments) =>
        new GitProcess(workingDirectory, arguments, telemetry);
}
