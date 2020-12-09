using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.Git.Diff;
using GitOut.Features.Git.Patch;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public sealed class LocalGitRepository : IGitRepository
    {
        private LocalGitRepository(DirectoryPath repositoryPath) => WorkingDirectory = repositoryPath;

        public DirectoryPath WorkingDirectory { get; }
        public string? Name => Path.GetFileName(WorkingDirectory.Directory);

        public GitStatusResult? CachedStatus { get; private set; }

        public async Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync(LogOptions options)
        {
            IDictionary<GitCommitId, GitHistoryEvent> historyByCommitId = new Dictionary<GitCommitId, GitHistoryEvent>();
            IList<GitHistoryEvent> history = new List<GitHistoryEvent>();
            IGitHistoryEventBuilder builder = GitHistoryEvent.Builder();
            int state = 0;
            var argumentBuilder = new StringBuilder("-c log.showSignature=false log -z --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\" --branches");
            if (options.IncludeRemotes)
            {
                argumentBuilder.Append(" --remotes");
            }
            IGitProcess log = CreateProcess(GitProcessOptions.FromArguments(argumentBuilder.ToString()));
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

            IGitProcess branches = CreateProcess(GitProcessOptions.FromArguments("for-each-ref --sort=-committerdate refs --format=\"%(objectname) %(refname)\""));
            await foreach (string line in branches.ReadLinesAsync())
            {
                var id = GitCommitId.FromHash(line.Substring(0, 40));
                if (historyByCommitId.TryGetValue(id, out GitHistoryEvent? logitem))
                {
                    var branch = GitBranchName.Create(line[41..]);
                    logitem.Branches.Add(branch);
                }
            }

            IGitProcess head = CreateProcess(GitProcessOptions.FromArguments("rev-parse HEAD"));
            await foreach (string line in head.ReadLinesAsync())
            {
                var id = GitCommitId.FromHash(line);
                historyByCommitId[id].IsHead = true;
            }

            return history;
        }

        public async Task<GitHistoryEvent> GetHeadAsync()
        {
            IGitProcess log = CreateProcess(GitProcessOptions.FromArguments("-c log.showSignature=false log -n1 -z --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\" HEAD"));
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

        public async IAsyncEnumerable<GitStash> ExecuteStashListAsync()
        {
            IGitProcess stashes = CreateProcess(GitProcessOptions.FromArguments("stash list -z"));
            await foreach (string line in stashes.ReadLinesAsync())
            {
                string[] stashEntries = line.Split('\0', StringSplitOptions.RemoveEmptyEntries);
                foreach (string stashLine in stashEntries)
                {
                    IGitStashBuilder stash = GitStash.Parse(stashLine);
                    IGitProcess getParentId = CreateProcess(GitProcessOptions.FromArguments($"rev-parse \"{stash.Name}~\""));
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
            IGitProcess status = CreateProcess(GitProcessOptions.FromArguments("--no-optional-locks status --porcelain=2 -z --untracked-files=all --ignore-submodules=none"));
            await foreach (string line in status.ReadLinesAsync())
            {
                string[] statusLines = line.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < statusLines.Length; ++i)
                {
                    string? statusChange = statusLines[i];
                    IGitStatusChangeBuilder builder = GitStatusChange.Parse(statusChange);
                    if (builder.Type == GitStatusChangeType.RenamedOrCopied)
                    {
                        builder.MergedFrom(statusLines[++i]);
                    }
                    statusChanges.Add(builder.Build());
                }
            }
            CachedStatus = new GitStatusResult(statusChanges);
            return CachedStatus;
        }

        public async Task<string[]> GetFileContentsAsync(GitFileId file)
        {
            var args = GitProcessOptions.FromArguments($"cat-file blob \"{file}\"");

            IGitProcess diff = CreateProcess(args);
            var content = new List<string>();
            await foreach (string line in diff.ReadLinesAsync())
            {
                content.Add(line);
            }
            return content.ToArray();
        }

        public async IAsyncEnumerable<GitDiffFileEntry> ExecuteListDiffChangesAsync(GitObjectId change, GitObjectId? parent)
        {
            string diffArguments = $"--no-optional-locks diff-tree --no-color {parent?.Hash ?? "-root"} {change} -z";
            IGitProcess diff = CreateProcess(GitProcessOptions.FromArguments(diffArguments));

            await foreach (string line in diff.ReadLinesAsync())
            {
                string[] diffLines = line.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < diffLines.Length; ++i)
                {
                    string fileLine = diffLines[i++];
                    string path = diffLines[i];
                    IGitDiffFileEntryBuilder builder = GitDiffFileEntry.Parse(fileLine);
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

        public async Task<GitDiffResult> ExecuteDiffAsync(GitObjectId source, GitObjectId target, DiffOptions options)
        {
            if (source.IsEmpty)
            {
                throw new ArgumentException("Cannot diff empty hash", nameof(source));
            }
            if (target.IsEmpty)
            {
                throw new ArgumentException("Cannot diff empty hash", nameof(target));
            }
            if (target.Equals(source))
            {
                throw new ArgumentException("Source and target is the same object, must diff different id's", nameof(target));
            }
            string argumentBuilder = $"--no-optional-locks diff --no-color {string.Join(" ", options.GetArguments(false))} {source} {target}";
            var args = GitProcessOptions.FromArguments(argumentBuilder.ToString());

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
            var args = GitProcessOptions.FromArguments(argumentBuilder);

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

        public async IAsyncEnumerable<GitFileEntry> ExecuteListFilesAsync(GitObjectId id)
        {
            IGitProcess process = CreateProcess(GitProcessOptions.FromArguments($"ls-tree -z {id.Hash}"));
            await foreach (string line in process.ReadLinesAsync())
            {
                string[] fileLines = line.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string? fileLine in fileLines)
                {
                    yield return GitFileEntry.Parse(fileLine);
                }
            }
        }

        public Task ExecuteAddAllAsync() => CreateProcess(GitProcessOptions.FromArguments("add --all")).ExecuteAsync();

        public Task ExecuteResetAllAsync() => CreateProcess(GitProcessOptions.FromArguments("reset HEAD")).ExecuteAsync();

        public Task ExecuteAddAsync(GitStatusChange change) => CreateProcess(GitProcessOptions.FromArguments($"add {change.Path}")).ExecuteAsync();

        public Task ExecuteResetAsync(GitStatusChange change) => CreateProcess(GitProcessOptions.FromArguments($"reset -- {change.Path}")).ExecuteAsync();

        public Task ExecuteCommitAsync(GitCommitOptions options)
        {
            var argumentsBuilder = new StringBuilder("commit");
            if (options.Amend)
            {
                argumentsBuilder.Append(" --amend");
            }
            argumentsBuilder.Append($" -m \"{options.Message.Replace("\"", "\\\"")}\"");
            return CreateProcess(GitProcessOptions.FromArguments(argumentsBuilder.ToString())).ExecuteAsync();
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

            IGitProcess apply = CreateProcess(GitProcessOptions.FromArguments(argumentsBuilder.ToString()));
            return apply.ExecuteAsync(patch.Writer);
        }

        public static LocalGitRepository InitializeFromPath(DirectoryPath path) => new LocalGitRepository(path);

        private IGitProcess CreateProcess(GitProcessOptions arguments) => new GitProcess(WorkingDirectory, arguments);
    }
}
