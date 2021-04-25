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

namespace GitOut.Features.Git
{
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

        public async Task<bool> IsInsideWorkTree()
        {
            IGitProcess proc = CreateProcess(ProcessOptions.Builder().AppendRange("rev-parse", "--is-inside-work-tree").Build());
            await foreach (string line in proc.ReadLinesAsync())
            {
                return line == "true";
            }
            return false;
        }

        public async Task<GitHistoryEvent> GetHeadAsync()
        {
            IGitProcess log = CreateProcess(ProcessOptions.FromArguments("-c log.showSignature=false log -n1 -z --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\" HEAD"));
            int state = 0;
            IGitHistoryEventBuilder builder = GitHistoryEvent.Builder();
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
                        int zeroSeparator = line.IndexOf('\0');
                        if (zeroSeparator != -1)
                        {
                            throw new InvalidOperationException("Multiple history events found but expected only 1");
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

        public Task ExecuteFetchAsync(GitRemote remote) => CreateProcess(ProcessOptions.FromArguments($"fetch {remote.Name}")).ExecuteAsync();

        public async Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync(LogOptions options)
        {
            IDictionary<GitCommitId, GitHistoryEvent> historyByCommitId = new Dictionary<GitCommitId, GitHistoryEvent>();
            IList<GitHistoryEvent> history = new List<GitHistoryEvent>();
            IGitHistoryEventBuilder builder = GitHistoryEvent.Builder();
            int state = 0;
            IProcessOptionsBuilder processOptionsBuilder = ProcessOptions
                .Builder()
                .AppendRange("-c", "log.showSignature=false", "log", "-z", "--date-order", "--pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\"", "--branches");

            if (options.IncludeRemotes)
            {
                processOptionsBuilder.Append(" --remotes");
            }
            IGitProcess log = CreateProcess(processOptionsBuilder.Build());
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
                        int zeroSeparator = line.IndexOf('\0');
                        if (zeroSeparator != -1)
                        {
                            string body = line[0..zeroSeparator];
                            builder.BuildBody(body);
                            string hashes = line[(zeroSeparator + 1)..];
                            if (hashes.Length == 0)
                            {
                                break;
                            }
                            GitHistoryEvent item = builder.Build();
                            history.Add(item);
                            historyByCommitId.Add(item.Id, item);
                            builder = GitHistoryEvent.Builder().ParseHash(hashes);
                            state = 1;
                        }
                        else
                        {
                            builder.BuildBody(line);
                        }
                        break;
                }
            }
            GitHistoryEvent lastItem = builder.Build();
            history.Add(lastItem);
            historyByCommitId.Add(lastItem.Id, lastItem);
            foreach (GitHistoryEvent children in history)
            {
                children.ResolveParents(historyByCommitId);
            }

            IGitProcess branches = CreateProcess(ProcessOptions.FromArguments("for-each-ref --sort=-committerdate refs --format=\"%(objectname) %(refname)\""));
            await foreach (string line in branches.ReadLinesAsync())
            {
                var id = GitCommitId.FromHash(line.Substring(0, 40));
                if (historyByCommitId.TryGetValue(id, out GitHistoryEvent? logitem))
                {
                    var branch = GitBranchName.Create(line[41..]);
                    logitem.Branches.Add(branch);
                }
            }

            IGitProcess head = CreateProcess(ProcessOptions.FromArguments("rev-parse HEAD"));
            await foreach (string line in head.ReadLinesAsync())
            {
                var id = GitCommitId.FromHash(line);
                if (historyByCommitId.TryGetValue(id, out GitHistoryEvent? value))
                {
                    value.IsHead = true;
                }
            }

            return history;
        }

        public async IAsyncEnumerable<GitStash> ExecuteStashListAsync()
        {
            IGitProcess stashes = CreateProcess(ProcessOptions.FromArguments("stash list -z"));
            await foreach (string line in stashes.ReadLinesAsync())
            {
                string[] stashEntries = line.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                foreach (string stashLine in stashEntries)
                {
                    IGitStashBuilder stash = GitStash.Parse(stashLine);
                    IGitProcess getParentId = CreateProcess(ProcessOptions.FromArguments($"rev-parse \"{stash.Name}~\""));
                    await foreach (string parentId in getParentId.ReadLinesAsync())
                    {
                        stash.UseParent(parentId);
                    }
                    yield return stash.Build();
                }
            }
        }

        public async Task<GitStatusResult> ExecuteStatusAsync()
        {
            IList<GitStatusChange> statusChanges = new List<GitStatusChange>();
            IGitProcess status = CreateProcess(ProcessOptions.FromArguments("--no-optional-locks status --porcelain=2 -z --untracked-files=all --ignore-submodules=none"));
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
                    statusChanges.Add(builder.Build());
                }
            }
            CachedStatus = new GitStatusResult(statusChanges);
            return CachedStatus;
        }

        public async Task<GitDiffResult> GetFileContentsAsync(GitFileId file)
        {
            var args = ProcessOptions.FromArguments($"cat-file blob \"{file}\"");

            IGitProcess diff = CreateProcess(args);
            IGitDiffBuilder builder = GitDiffResult.Builder();
            var content = new List<string>();
            await foreach (string line in diff.ReadLinesAsync())
            {
                content.Add(line);
            }
            builder.Feed($"{GitDiffHunk.HunkIdentifier} -1,{content.Count} +1,{content.Count} {GitDiffHunk.HunkIdentifier}");
            foreach (string line in content)
            {
                builder.Feed($" {line}");
            }
            return builder.Build();
        }

        public async IAsyncEnumerable<GitDiffFileEntry> ExecuteListDiffChangesAsync(GitObjectId change, GitObjectId? parent, DiffOptions? options)
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
                string[] diffLines = line.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
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
                    if (builder.Type == GitDiffType.CopyEdit || builder.Type == GitDiffType.RenameEdit)
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

        public async Task<GitDiffResult> ExecuteDiffAsync(GitFileId source, GitFileId target, DiffOptions options)
        {
            if (source.IsEmpty)
            {
                return await GetFileContentsAsync(target);
            }
            if (target.IsEmpty)
            {
                return await GetFileContentsAsync(source);
            }
            if (target.Equals(source))
            {
                throw new ArgumentException("Source and target is the same object, must diff different id's", nameof(target));
            }
            string argumentBuilder = $"--no-optional-locks diff --no-color {string.Join(" ", options.GetArguments(false))} {source} {target}";
            var args = ProcessOptions.FromArguments(argumentBuilder.ToString());

            IGitProcess diff = CreateProcess(args);
            IGitDiffBuilder builder = GitDiffResult.Builder();
            await foreach (string line in diff.ReadLinesAsync())
            {
                builder.Feed(line);
            }
            return builder.Build();
        }

        public async Task<GitDiffResult> ExecuteDiffAsync(RelativeDirectoryPath file, DiffOptions options)
        {
            string argumentBuilder = $"--no-optional-locks diff --no-color {string.Join(" ", options.GetArguments())} -- {file}";
            var args = ProcessOptions.FromArguments(argumentBuilder);

            IGitProcess diff = CreateProcess(args);
            IGitDiffBuilder builder = GitDiffResult.Builder();
            await foreach (string line in diff.ReadLinesAsync())
            {
                builder.Feed(line);
            }
            return builder.Build();
        }

        public async Task<GitDiffResult> ExecuteUntrackedDiffAsync(RelativeDirectoryPath path)
        {
            string[] result = await File.ReadAllLinesAsync(Path.Combine(WorkingDirectory.Directory, path.ToString()));
            IGitDiffBuilder builder = GitDiffResult.Builder();
            builder.Feed($"{GitDiffHunk.HunkIdentifier} -0,0 +1,{result.Length} {GitDiffHunk.HunkIdentifier}");
            foreach (string line in result)
            {
                builder.Feed($"+{line}");
            }
            return builder.Build();
        }

        public async IAsyncEnumerable<GitFileEntry> ExecuteListTreeAsync(GitObjectId id, DiffOptions? options)
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
                string[] fileLines = line.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string? fileLine in fileLines)
                {
                    yield return GitFileEntry.Parse(fileLine);
                }
            }
        }

        public Task ExecuteAddAllAsync() => CreateProcess(ProcessOptions.FromArguments("add --all")).ExecuteAsync();

        public Task ExecuteResetAllAsync() => CreateProcess(ProcessOptions.FromArguments("reset HEAD")).ExecuteAsync();

        public Task ExecuteAddAsync(GitStatusChange change, AddOptions options) => CreateProcess(ProcessOptions.FromArguments($"add {string.Join(" ", options.GetArguments())} -- {change.Path}")).ExecuteAsync();

        public Task ExecuteCheckoutAsync(GitStatusChange change) => CreateProcess(ProcessOptions.FromArguments($"checkout HEAD -- {change.Path}")).ExecuteAsync();

        public async Task ExecuteCheckoutBranchAsync(GitBranchName name)
        {
            ProcessEventArgs args = await CreateProcess(ProcessOptions.FromArguments($"checkout -b {name.Name}")).ExecuteAsync();
            if (args.ErrorLines.Count > 0 && !args.ErrorLines.First().StartsWith("Switched to a new branch"))
            {
                throw new InvalidOperationException($"Could not create branch: {args.Error}");
            }
        }

        public Task ExecuteResetAsync(GitStatusChange change) => CreateProcess(ProcessOptions.FromArguments($"reset -- {change.Path}")).ExecuteAsync();

        public Task ExecuteCommitAsync(GitCommitOptions options)
        {
            var argumentsBuilder = new StringBuilder("commit");
            if (options.Amend)
            {
                argumentsBuilder.Append(" --amend");
            }
            argumentsBuilder.Append($" -m \"{options.Message.Replace("\"", "\\\"")}\"");
            return CreateProcess(ProcessOptions.FromArguments(argumentsBuilder.ToString())).ExecuteAsync();
        }

        public Task ExecuteApplyAsync(GitPatch patch)
        {
            var argumentsBuilder = new StringBuilder("apply --ignore-whitespace");
            switch (patch.Mode)
            {
                case PatchMode.AddIndex:
                case PatchMode.ResetIndex:
                    argumentsBuilder.Append(" --cached");
                    break;
            }

            IGitProcess apply = CreateProcess(ProcessOptions.FromArguments(argumentsBuilder.ToString()));
            return apply.ExecuteAsync(patch.Writer);
        }

        public static LocalGitRepository InitializeFromPath(
            DirectoryPath path,
            IProcessFactory<IGitProcess> processFactory
        ) => new(path, processFactory);

        private IGitProcess CreateProcess(ProcessOptions arguments) => processFactory.Create(WorkingDirectory, arguments);
    }
}
