using System.Collections.Generic;
using System.IO;
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

        public async Task<IEnumerable<GitHistoryEvent>> ExecuteLogAsync()
        {
            IDictionary<GitCommitId, GitHistoryEvent> historyByCommitId = new Dictionary<GitCommitId, GitHistoryEvent>();
            IList<GitHistoryEvent> history = new List<GitHistoryEvent>();
            IGitHistoryEventBuilder builder = GitHistoryEvent.Builder();
            int state = 0;
            IGitProcess log = Execute(GitProcessOptions.FromArguments("-c log.showSignature=false log -z --all --pretty=format:\"%H%P%n%at%n%an%n%ae%n%s%n%b\""));
            await foreach (string line in log.ReadLines())
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

            IGitProcess branches = Execute(GitProcessOptions.FromArguments("for-each-ref --sort=-committerdate refs/heads/ --format=\"%(objectname) %(refname)\""));
            await foreach (string line in branches.ReadLines())
            {
                var id = GitCommitId.FromHash(line.Substring(0, 40));
                var branch = GitBranchName.Create(line.Substring(52));
                historyByCommitId[id].Branches.Add(branch);
            }

            IGitProcess head = Execute(GitProcessOptions.FromArguments("rev-parse HEAD"));
            await foreach (string line in head.ReadLines())
            {
                var id = GitCommitId.FromHash(line);
                historyByCommitId[id].IsHead = true;
            }

            return history;
        }

        private IGitProcess Execute(GitProcessOptions arguments) => new GitProcess(WorkingDirectory, arguments);

        public static LocalGitRepository InitializeFromPath(DirectoryPath path) => new LocalGitRepository(path);
    }

}
