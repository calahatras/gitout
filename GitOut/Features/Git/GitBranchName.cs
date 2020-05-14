using System;

namespace GitOut.Features.Git
{
    public class GitBranchName
    {
        private GitBranchName(string type, string name)
        {
            if (name.Length <= 1)
            {
                throw new ArgumentException("Name must be longer than one character", nameof(name));
            }
            Type = type;
            Name = name;
            IconResource = type switch
            {
                "heads" => "SourceCommitLocal",
                "remotes" => "CheckNetwork",
                _ => "Archive"
            };
        }

        public string Type { get; }
        public string Name { get; }

        public string IconResource { get; }

        public static GitBranchName Create(string name)
        {
            if (!name.StartsWith("refs"))
            {
                throw new ArgumentException($"Invalid start of branch name {name}", nameof(name));
            }
            string[] parts = name.Split('/', 3);
            return parts.Length > 2
                ? new GitBranchName(parts[1], parts[2])
                : new GitBranchName(parts[1], parts[1]);
        }
    }
}
