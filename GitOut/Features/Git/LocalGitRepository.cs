using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GitOut.Features.Diagnostics;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;
using GitOut.Features.Memory;

namespace GitOut.Features.Git;

public sealed class LocalGitRepository : IGitRepository
{
    private readonly IProcessFactory<IGitProcess> processFactory;

    private LocalGitRepository(
        DirectoryPath repositoryPath,
        IProcessFactory<IGitProcess> processFactory
    )
    {
        WorkingDirectory = repositoryPath;
        this.processFactory = processFactory;
    }

    public DirectoryPath WorkingDirectory { get; }
    public string Name => Path.GetFileName(WorkingDirectory.Directory)!;

    public GitStatusResult? CachedStatus { get; private set; }

    private static readonly char[] GitLineSeparator = new char[] { '\0' };

    public async Task<bool> IsInsideWorkTree()
    {
        IGitProcess proc = CreateProcess(
            ProcessOptions.Builder().AppendRange("rev-parse", "--is-inside-work-tree").Build()
        );
        await foreach (string line in proc.ReadLinesAsync())
        {
            return line == "true";
        }
        return false;
    }

    public async Task<GitCommitId?> GetCommitIdAsync(string reference)
    {
        await foreach (
            string line in CreateProcess(ProcessOptions.FromArguments($"rev-parse {reference}"))
                .ReadLinesAsync()
        )
        {
            return GitCommitId.FromHash(line);
        }
        return null;
    }

    public async Task<GitHistoryEvent> GetHeadAsync()
    {
        IGitProcess log = CreateProcess(
            ProcessOptions.FromArguments(
                "-c log.showSignature=false log -n1 -z --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\" HEAD"
            )
        );
        int state = 0;
        IGitHistoryEventBuilder<GitHistoryEvent> builder = GitHistoryEvent.Builder();
        await foreach (string line in log.ReadLinesAsync())
        {
            switch (state)
            {
                case 0:
                    builder.ParseHash(line);
                    ++state;
                    break;
                case 1:
                    builder.ParseDate(long.Parse(line));
                    ++state;
                    break;
                case 2:
                    builder.ParseAuthorName(line);
                    ++state;
                    break;
                case 3:
                    builder.ParseAuthorEmail(line);
                    ++state;
                    break;
                case 4:
                    builder.ParseSubject(line);
                    ++state;
                    break;
                case 5:
                    if (line.Contains('\0', StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Multiple history events found but expected only 1"
                        );
                    }
                    else
                    {
                        builder.BuildBody(line);
                    }
                    break;
            }
        }
        return builder.Build();
    }

    public async IAsyncEnumerable<GitRemote> GetRemotesAsync()
    {
        IGitProcess remotes = CreateProcess(ProcessOptions.FromArguments("remote"));
        await foreach (string line in remotes.ReadLinesAsync())
        {
            yield return new GitRemote(line);
        }
    }

    public Task FetchAsync(GitRemote remote) =>
        CreateProcess(ProcessOptions.FromArguments($"fetch {remote.Name}")).ExecuteAsync();

    public Task PruneRemoteAsync(GitRemote remote) =>
        CreateProcess(ProcessOptions.FromArguments($"remote prune {remote.Name}")).ExecuteAsync();

    public async Task<IEnumerable<GitHistoryEvent>> LogAsync(LogOptions options)
    {
        var historyByCommitId = new Dictionary<GitCommitId, GitHistoryEvent>();
        var history = new List<GitHistoryEvent>();
        IProcessOptionsBuilder processOptionsBuilder = ProcessOptions
            .Builder()
            .AppendRange(
                "-c",
                "log.showSignature=false",
                "log",
                "-z",
                "--date-order",
                "--pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\"",
                "--branches"
            );

        if (options.IncludeRemotes)
        {
            processOptionsBuilder.Append(" --remotes");
        }
        IGitProcess log = CreateProcess(processOptionsBuilder.Build());
        await foreach (
            GitHistoryEvent item in ParseHistoryLines(log.ReadLinesAsync(), GitHistoryEvent.Builder)
        )
        {
            history.Add(item);
            historyByCommitId.Add(item.Id, item);
        }
        if (options.IncludeStashes)
        {
            IGitProcess stashes = CreateProcess(
                ProcessOptions.FromArguments(
                    "stash list --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\" -z"
                )
            );
            int i = 0;
            await foreach (GitHistoryEvent item in StashListAsync())
            {
                if (historyByCommitId.TryGetValue(item.ParentId!, out GitHistoryEvent? parent))
                {
                    history.Insert(history.IndexOf(parent), item);
                    item.Branches.Add(GitBranchName.Create($"refs/stash@{{{i}}}"));
                }
                ++i;
            }
        }
        foreach (GitHistoryEvent children in history)
        {
            children.ResolveParents(historyByCommitId);
        }

        IGitProcess branches = CreateProcess(
            ProcessOptions.FromArguments(
                "for-each-ref --sort=-committerdate refs --format=\"%(objectname) %(refname)\""
            )
        );
        await foreach (string line in branches.ReadLinesAsync())
        {
            var id = GitCommitId.FromHash(line.AsSpan()[..40]);
            if (historyByCommitId.TryGetValue(id, out GitHistoryEvent? logitem))
            {
                var branch = GitBranchName.Create(line[41..]);
                logitem.Branches.Add(branch);
            }
        }
        GitCommitId? head = await GetCommitIdAsync("HEAD");
        if (head is not null)
        {
            if (historyByCommitId.TryGetValue(head, out GitHistoryEvent? value))
            {
                value.IsHead = true;
            }
        }

        return history;
    }

    public async IAsyncEnumerable<GitStash> StashListAsync()
    {
        IGitProcess stashes = CreateProcess(
            ProcessOptions.FromArguments(
                "stash list --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\" -z"
            )
        );
        int i = 0;
        await foreach (
            GitStash item in ParseHistoryLines(stashes.ReadLinesAsync(), () => GitStash.Builder(i))
        )
        {
            ++i;
            yield return item;
        }
    }

    public async Task<GitStatusResult> StatusAsync()
    {
        var statusChanges = new List<GitStatusChange>();
        IGitProcess status = CreateProcess(
            ProcessOptions.FromArguments(
                "--no-optional-locks status --porcelain=2 -z --untracked-files=all --ignore-submodules=none"
            )
        );
        await foreach (string line in status.ReadLinesAsync())
        {
            Range[] ranges = line.AsSpan().Split('\0', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < ranges.Length; ++i)
            {
                IGitStatusChangeBuilder builder = GitStatusChange.Parse(line[ranges[i]]);
                if (builder.Type == GitStatusChangeType.RenamedOrCopied)
                {
                    builder.MergedFrom(line[ranges[++i]]);
                }
                builder.WorkingDirectory(WorkingDirectory);
                statusChanges.Add(builder.Build());
            }
        }
        CachedStatus = new GitStatusResult(statusChanges);
        return CachedStatus;
    }

    public Stream GetUntrackedBlobStream(RelativeDirectoryPath path) =>
        File.OpenRead(Path.Combine(WorkingDirectory.Directory, path.ToString()));

    public Task<Stream> GetBlobStreamAsync(GitFileId file) =>
        CreateProcess(ProcessOptions.FromArguments($"cat-file blob \"{file}\"")).ReadStreamAsync();

    public async IAsyncEnumerable<GitDiffFileEntry> ListDiffChangesAsync(
        GitObjectId change,
        GitObjectId? parent,
        DiffOptions? options
    )
    {
        IProcessOptionsBuilder diffArguments = ProcessOptions
            .Builder()
            .AppendRange("--no-optional-locks", "diff-tree", "--no-color", "-z");

        if (options is not null)
        {
            diffArguments.AppendRange(options.GetArguments());
        }

        bool shouldSkip = false;
        if (parent is null)
        {
            diffArguments.AppendRange(change.ToString(), "--root");
            shouldSkip = true;
        }
        else
        {
            diffArguments.AppendRange(parent.Hash, change.ToString());
        }
        IGitProcess diff = CreateProcess(diffArguments.Build());

        await foreach (string line in diff.ReadLinesAsync())
        {
            string[] diffLines = line.Split(
                GitLineSeparator,
                StringSplitOptions.RemoveEmptyEntries
            );
            if (shouldSkip)
            {
                shouldSkip = false;
                diffLines = diffLines[1..];
            }
            for (int i = 0; i < diffLines.Length; ++i)
            {
                string fileLine = diffLines[i++];
                string path = diffLines[i];
                IGitDiffFileEntryBuilder builder = GitDiffFileEntry.Parse(fileLine.AsSpan());
                if (builder.Type is GitDiffType.CopyEdit or GitDiffType.RenameEdit)
                {
                    yield return builder.Build(path, diffLines[i + 1]);
                    ++i;
                }
                else
                {
                    yield return builder.Build(path);
                }
            }
        }
    }

    public async Task<GitDiffResult> DiffAsync(
        GitFileId source,
        GitFileId target,
        DiffOptions options
    )
    {
        IGitDiffBuilder builder = GitDiffResult.Builder();
        if (source.IsEmpty)
        {
            return builder.Feed(await GetBlobStreamAsync(target)).Build();
        }
        if (target.IsEmpty)
        {
            return builder.Feed(await GetBlobStreamAsync(source)).Build();
        }
        if (target.Equals(source))
        {
            throw new ArgumentException(
                "Source and target is the same object, must diff different id's",
                nameof(target)
            );
        }
        string argumentBuilder =
            $"--no-optional-locks diff --no-color {string.Join(" ", options.GetArguments(false))} {source} {target}";
        var args = ProcessOptions.FromArguments(argumentBuilder.ToString());

        IGitProcess diff = CreateProcess(args);
        await foreach (string line in diff.ReadLinesAsync())
        {
            builder.Feed(line);
        }
        if (builder.IsBinaryFile)
        {
            builder.Feed(await GetBlobStreamAsync(target));
        }
        return builder.Build();
    }

    public async Task<GitDiffResult> DiffAsync(RelativeDirectoryPath file, DiffOptions options)
    {
        string argumentBuilder =
            $"--no-optional-locks diff --no-color {string.Join(" ", options.GetArguments())} -- {file}";
        var args = ProcessOptions.FromArguments(argumentBuilder);

        IGitProcess diff = CreateProcess(args);
        IGitDiffBuilder builder = GitDiffResult.Builder();
        await foreach (string line in diff.ReadLinesAsync())
        {
            builder.Feed(line);
        }
        if (builder.IsBinaryFile)
        {
            builder.Feed(GetUntrackedBlobStream(file));
        }
        return builder.Build();
    }

    public async Task<GitDiffResult> DiffAsync(
        RelativeDirectoryPath source,
        RelativeDirectoryPath destination,
        DiffOptions options
    )
    {
        string argumentBuilder =
            $"--no-optional-locks diff --no-index --no-color {string.Join(" ", options.GetArguments())} -- {source} {destination}";
        var args = ProcessOptions.FromArguments(argumentBuilder);

        IGitProcess diff = CreateProcess(args);
        IGitDiffBuilder builder = GitDiffResult.Builder();
        await foreach (string line in diff.ReadLinesAsync())
        {
            builder.Feed(line);
        }
        if (builder.IsBinaryFile)
        {
            builder.Feed(GetUntrackedBlobStream(destination));
        }
        return builder.Build();
    }

    public async IAsyncEnumerable<GitFileEntry> ListTreeAsync(GitObjectId id, DiffOptions? options)
    {
        IProcessOptionsBuilder builder = ProcessOptions.Builder().AppendRange("ls-tree", "-z");
        if (options is not null)
        {
            builder.AppendRange(options.GetArguments());
        }
        builder.Append(id.Hash);
        IGitProcess process = CreateProcess(builder.Build());
        await foreach (string line in process.ReadLinesAsync())
        {
            string[] fileLines = line.Split(
                GitLineSeparator,
                StringSplitOptions.RemoveEmptyEntries
            );
            foreach (string? fileLine in fileLines)
            {
                yield return GitFileEntry.Parse(fileLine);
            }
        }
    }

    public Task AddAllAsync() =>
        CreateProcess(ProcessOptions.FromArguments("add --all")).ExecuteAsync();

    public Task ResetAllAsync() =>
        CreateProcess(ProcessOptions.FromArguments("reset HEAD")).ExecuteAsync();

    public Task ResetToCommitAsync(GitCommitId id) =>
        CreateProcess(ProcessOptions.FromArguments($"reset {id}")).ExecuteAsync();

    public Task AddAsync(GitStatusChange change, AddOptions options) =>
        CreateProcess(
                ProcessOptions.FromArguments(
                    $"add {string.Join(" ", options.BuildArguments())} -- {change.Path}"
                )
            )
            .ExecuteAsync();

    public Task CheckoutAsync(GitStatusChange change) =>
        CreateProcess(ProcessOptions.FromArguments($"checkout HEAD -- {change.Path}"))
            .ExecuteAsync();

    public async Task CreateBranchAsync(
        GitBranchName name,
        GitCreateBranchOptions? options = default
    )
    {
        IProcessOptionsBuilder arguments = ProcessOptions
            .Builder()
            .Append("branch")
            .Append(name.Name);
        if (options is not null)
        {
            arguments.Append(options.From.ToString());
        }
        ProcessEventArgs args = await CreateProcess(arguments.Build()).ExecuteAsync();
        if (args.ErrorLines.Count > 0)
        {
            throw new InvalidOperationException($"Could not create branch: {args.Error}");
        }
    }

    public async Task<GitDeleteResult> DeleteBranchAsync(
        GitBranchName name,
        GitDeleteBranchOptions? options = default
    )
    {
        ProcessOptions arguments = ProcessOptions
            .Builder()
            .Append("branch")
            .Append(options?.ForceDelete ?? false ? "--delete --force" : "--delete")
            .Append(name.Name)
            .Build();
        ProcessEventArgs args = await CreateProcess(arguments).ExecuteAsync();
        return GitDeleteResult.Parse(name, this, args.OutputLines, args.ErrorLines);
    }

    public async Task CheckoutCommitDetachedAsync(GitCommitId id)
    {
        ProcessEventArgs args = await CreateProcess(
                ProcessOptions.FromArguments($"checkout {id} --detach")
            )
            .ExecuteAsync();
        if (args.ErrorLines.Count > 0)
        {
            string message = args.ErrorLines.First();
            if (
                message.StartsWith("Note: switching to ")
                || message.StartsWith("HEAD is now at ")
                || message.StartsWith("Previous HEAD position was ")
            )
            {
                return;
            }
            if (
                message
                == "error: Your local changes to the following files would be overwritten by checkout:"
            )
            {
                throw new InvalidOperationException(
                    $"Could not checkout commit due to edits: {args.Error}"
                );
            }
            else
            {
                throw new InvalidOperationException($"Could not checkout commit: {args.Error}");
            }
        }
    }

    public async Task CheckoutBranchAsync(
        GitBranchName name,
        GitCheckoutBranchOptions? options = default
    )
    {
        IProcessOptionsBuilder arguments = ProcessOptions.Builder();
        arguments.Append("checkout");
        if (options is not null && options.CreateBranch)
        {
            arguments.Append("-b");
        }
        arguments.Append(name.Name);
        ProcessEventArgs args = await CreateProcess(arguments.Build()).ExecuteAsync();
        if (args.ErrorLines.Count > 0)
        {
            string message = args.ErrorLines.First();
            if (
                message == $"Switched to branch '{name.Name}'"
                || message == $"Switched to a new branch '{name.Name}'"
                || message == $"Already on '{name}'"
                || message.StartsWith("Previous HEAD position was ")
            )
            {
                return;
            }
            if (message == $"fatal: A branch named '{name}' already exists.")
            {
                throw new InvalidOperationException($"Could not create branch: {args.Error}");
            }
            if (
                message
                == "error: Your local changes to the following files would be overwritten by checkout:"
            )
            {
                throw new InvalidOperationException(
                    $"Could not checkout branch due to edits: {args.Error}"
                );
            }
            else
            {
                throw new InvalidOperationException($"Could not checkout branch: {args.Error}");
            }
        }
    }

    public Task ResetAsync(GitStatusChange change) =>
        CreateProcess(ProcessOptions.FromArguments($"reset -- {change.Path}")).ExecuteAsync();

    public Task RestoreAsync(GitStatusChange change) =>
        CreateProcess(ProcessOptions.FromArguments($"restore --staged -- {change.Path}"))
            .ExecuteAsync();

    public Task StashIndexAsync() =>
        CreateProcess(ProcessOptions.FromArguments("stash --staged")).ExecuteAsync();

    public Task CommitAsync(GitCommitOptions options)
    {
        var argumentsBuilder = new StringBuilder("commit");
        if (options.Amend)
        {
            argumentsBuilder.Append(" --amend");
        }
        argumentsBuilder.Append(
            $" -m \"{options.Message.Replace("\"", "\\\"", StringComparison.OrdinalIgnoreCase)}\""
        );
        return CreateProcess(ProcessOptions.FromArguments(argumentsBuilder.ToString()))
            .ExecuteAsync();
    }

    public Task RestoreWorkspaceAsync(GitStatusChange change) =>
        CreateProcess(ProcessOptions.FromArguments($"restore -- {change.Path}")).ExecuteAsync();

    public Task ApplyAsync(GitPatch patch)
    {
        var argumentsBuilder = new StringBuilder("apply --ignore-whitespace");
        switch (patch.Mode)
        {
            case PatchMode.AddIndex:
            case PatchMode.ResetIndex:
                argumentsBuilder.Append(" --cached");
                break;
        }

        IGitProcess apply = CreateProcess(
            ProcessOptions.FromArguments(argumentsBuilder.ToString())
        );
        return apply.ExecuteAsync(patch.Writer);
    }

    public static LocalGitRepository InitializeFromPath(
        DirectoryPath path,
        IProcessFactory<IGitProcess> processFactory
    ) => new(path, processFactory);

    private IGitProcess CreateProcess(ProcessOptions arguments) =>
        processFactory.Create(WorkingDirectory, arguments);

    private static async IAsyncEnumerable<T> ParseHistoryLines<T>(
        IAsyncEnumerable<string> lines,
        Func<IGitHistoryEventBuilder<T>> factory
    )
        where T : GitHistoryEvent
    {
        IGitHistoryEventBuilder<T> builder = factory();
        int state = 0;
        await foreach (string line in lines)
        {
            switch (state)
            {
                case 0:
                    builder.ParseHash(line);
                    ++state;
                    break;
                case 1:
                    builder.ParseDate(long.Parse(line));
                    ++state;
                    break;
                case 2:
                    builder.ParseAuthorName(line);
                    ++state;
                    break;
                case 3:
                    builder.ParseAuthorEmail(line);
                    ++state;
                    break;
                case 4:
                    builder.ParseSubject(line);
                    ++state;
                    break;
                case 5:
                    int zeroSeparator = line.IndexOf('\0', StringComparison.OrdinalIgnoreCase);
                    if (zeroSeparator != -1)
                    {
                        string body = line[0..zeroSeparator];
                        builder.BuildBody(body);
                        string hashes = line[(zeroSeparator + 1)..];
                        if (hashes.Length == 0)
                        {
                            break;
                        }
                        T item = builder.Build();
                        yield return item;
                        builder = factory();
                        builder.ParseHash(hashes);
                        state = 1;
                    }
                    else
                    {
                        builder.BuildBody(line);
                    }
                    break;
            }
        }
        if (state != 0)
        {
            T lastItem = builder.Build();
            yield return lastItem;
        }
    }
}
