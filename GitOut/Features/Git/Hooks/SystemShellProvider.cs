using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitOut.Features.Git.Hooks;

public class SystemShellProvider : IShellProvider
{
    public async IAsyncEnumerable<ShellPath> FindAvailableShellsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        string? programFiles =
            Environment.GetEnvironmentVariable("ProgramW6432")
            ?? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string system32 = Environment.GetFolderPath(Environment.SpecialFolder.System);

        string pwshPath = Path.Combine(programFiles, "PowerShell", "7", "pwsh.exe");
        if (File.Exists(pwshPath))
        {
            yield return new ShellPath(pwshPath, "PowerShell Core");
        }

        string powershellPath = Path.Combine(
            system32,
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe"
        );
        if (File.Exists(powershellPath))
        {
            yield return new ShellPath(powershellPath, "Windows PowerShell");
        }

        string gitBashPath = Path.Combine(programFiles, "Git", "bin", "bash.exe");
        if (File.Exists(gitBashPath))
        {
            yield return new ShellPath(gitBashPath, "Git Bash");
        }

        string wslBashPath = Path.Combine(system32, "bash.exe");
        if (File.Exists(wslBashPath))
        {
            yield return new ShellPath(wslBashPath, "WSL Bash");
        }

        string cmdPath = Path.Combine(system32, "cmd.exe");
        if (File.Exists(cmdPath))
        {
            yield return new ShellPath(cmdPath, "Command Prompt");
        }

        await Task.CompletedTask;
    }
}
