using System.Collections.Generic;

namespace GitOut.Features.Git.Diagnostics
{
    public class GitProcessOptions
    {
        private GitProcessOptions(string arguments) => Arguments = arguments;

        public string Arguments { get; }

        public static GitProcessOptions FromArguments(string arguments) => new GitProcessOptions(arguments);

        public static IGitProcessOptionsBuilder Builder() => new GitProcessOptionsBuilder();

        private class GitProcessOptionsBuilder : IGitProcessOptionsBuilder
        {
            private readonly List<string> arguments = new List<string>();

            public IGitProcessOptionsBuilder Append(string argument)
            {
                arguments.Add(argument);
                return this;
            }

            public IGitProcessOptionsBuilder AppendRange(params string[] collection)
            {
                arguments.AddRange(collection);
                return this;
            }

            public IGitProcessOptionsBuilder AppendRange(IEnumerable<string> collection)
            {
                arguments.AddRange(collection);
                return this;
            }

            public GitProcessOptions Build() => new GitProcessOptions(string.Join(" ", arguments));
        }
    }
}
