using System;
using System.Text.RegularExpressions;

namespace GitOut.Features.Git
{
    public class GitBranchName
    {
        private const string LocalBranchType = "heads";
        private const string RemoteBranchType = "remotes";

        private static readonly Regex ValidBranchName = new("^[\\w\\d](?:[\\w\\d-+@&\\/\\{}.]+)(?<![\\/\\.])$");
        private GitBranchName(string type, string name)
        {
            if (name.Length <= 1)
            {
                throw new ArgumentException("Name must be longer than one character", nameof(name));
            }
            if (!IsValid(name))
            {
                throw new ArgumentException("Name is not a valid branch name match", nameof(name));
            }
            Type = type;
            Name = name;
            IconResource = type switch
            {
                LocalBranchType => "SourceCommitLocal",
                RemoteBranchType => "CheckNetwork",
                _ => "Archive"
            };
        }

        public string Type { get; }
        public string Name { get; }

        public string IconResource { get; }

        public static bool IsValid(string? name) => name is not null && name.Length > 1 && ValidBranchName.IsMatch(name);

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

        public static GitBranchName CreateLocal(string name) => new(LocalBranchType, name);
    }
}
