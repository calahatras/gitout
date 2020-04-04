namespace GitOut.Features.Git.Diagnostics
{
    public class GitProcessOptions
    {
        private GitProcessOptions(string arguments) => Arguments = arguments;

        public string Arguments { get; }

        public static GitProcessOptions FromArguments(string arguments) => new GitProcessOptions(arguments);
    }
}
