using System;

namespace GitOut.Features.Git
{
    public class GitCommitOptions
    {
        private GitCommitOptions(bool amend, string message)
        {
            if (message.Length == 0)
            {
                throw new ArgumentException("Message must be more than 0 characters", nameof(message));
            }
            Amend = amend;
            Message = message;
        }

        public bool Amend { get; }
        public string Message { get; }

        public static GitCommitOptions AmendLatest(string message) => new GitCommitOptions(true, message);
        public static GitCommitOptions CreateCommit(string message) => new GitCommitOptions(false, message);
    }
}
