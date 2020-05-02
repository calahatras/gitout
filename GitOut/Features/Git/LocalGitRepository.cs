using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using GitOut.Features.Git.Diagnostics;
using GitOut.Features.IO;

namespace GitOut.Features.Git
{
    public sealed class LocalGitRepository : IGitRepository
    {
        private LocalGitRepository(DirectoryPath repositoryPath) => WorkingDirectory = repositoryPath;

        public DirectoryPath WorkingDirectory { get; }
        public string? Name => Path.GetFileName(WorkingDirectory.Directory);

        public GitStatusResult? CachedStatus { get; private set; }

        public async Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync()
        {
            IDictionary<GitCommitId, GitHistoryEvent> historyByCommitId = new Dictionary<GitCommitId, GitHistoryEvent>();
            IList<GitHistoryEvent> history = new List<GitHistoryEvent>();
            IGitHistoryEventBuilder builder = GitHistoryEvent.Builder();
            int state = 0;
            IGitProcess log = CreateProcess(GitProcessOptions.FromArguments("-c log.showSignature=false log -z --all --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\""));
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
                            string body = line.Substring(0, zeroSeparator);
                            builder.BuildBody(body);
                            string hashes = line.Substring(zeroSeparator + 1);
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

            IGitProcess branches = CreateProcess(GitProcessOptions.FromArguments("for-each-ref --sort=-committerdate refs/heads/ --format=\"%(objectname) %(refname)\""));
            await foreach (string line in branches.ReadLinesAsync())
            {
                var id = GitCommitId.FromHash(line.Substring(0, 40));
                var branch = GitBranchName.Create(line.Substring(52));
                historyByCommitId[id].Branches.Add(branch);
            }

            IGitProcess head = CreateProcess(GitProcessOptions.FromArguments("rev-parse HEAD"));
            await foreach (string line in head.ReadLinesAsync())
            {
                var id = GitCommitId.FromHash(line);
                historyByCommitId[id].IsHead = true;
            }

            return history;
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

        public async Task<GitDiffResult> ExecuteDiffAsync(GitStatusChange change, DiffOptions options)
        {
            var argumentBuilder = new StringBuilder("--no-optional-locks diff --no-color ");
            if (options.Cached)
            {
                argumentBuilder.Append("--cached ");
            }
            if (options.IgnoreAllSpace)
            {
                argumentBuilder.Append("--ignore-all-space ");
            }
            argumentBuilder.Append($"-- {change.Path}");
            var args = GitProcessOptions.FromArguments(argumentBuilder.ToString());

            IGitProcess diff = CreateProcess(args);
            IGitDiffBuilder builder = GitDiffResult.ResultFor(change, options);
            await foreach (string line in diff.ReadLinesAsync())
            {
                builder.Feed(line);
            }
            return builder.Build();
        }

        public Task ExecuteAddAllAsync() => CreateProcess(GitProcessOptions.FromArguments("add --all")).ExecuteAsync();

        public Task ExecuteResetAllAsync() => CreateProcess(GitProcessOptions.FromArguments("reset HEAD")).ExecuteAsync();

        public Task ExecuteAddAsync(GitStatusChange change) => CreateProcess(GitProcessOptions.FromArguments($"add {change.Path}")).ExecuteAsync();

        public Task ExecuteResetAsync(GitStatusChange change) => CreateProcess(GitProcessOptions.FromArguments($"reset -- {change.Path}")).ExecuteAsync();

        public Task ExecuteCommitAsync(string message) => CreateProcess(GitProcessOptions.FromArguments($"commit -m \"{message.Replace("\"", "\\\"")}\"")).ExecuteAsync();

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
