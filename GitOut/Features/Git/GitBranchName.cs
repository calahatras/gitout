using System;

namespace GitOut.Features.Git
{
    public class GitBranchName
    {
        private GitBranchName(string name)
        {
            if (name.Length <= 1)
            {
                throw new ArgumentException("Name must be longer than one character", nameof(name));
            }
            Name = name;
        }

        public string Name { get; }

        public static GitBranchName Create(string name) => new GitBranchName(name);
    }
}
