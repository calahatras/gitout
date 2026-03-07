using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GitOut.Features.Git.Hooks;

public class SystemShellProviderTest
{
    [Test]
    public async Task FindAvailableShellsAsync_ShouldYieldAtLeastOneShell()
    {
        var provider = new SystemShellProvider();

        var shells = new System.Collections.Generic.List<ShellPath>();
        await foreach (ShellPath shell in provider.FindAvailableShellsAsync(CancellationToken.None))
        {
            shells.Add(shell);
        }

        // This is highly likely true on any real Windows machine (cmd or powershell will exist),
        // but test might be brittle in completely isolated environments without standard tools.
        // Assuming CI has at least one of these.
        Assert.That(shells.Count, Is.GreaterThan(0));
    }
}
