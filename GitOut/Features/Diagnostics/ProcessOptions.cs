using System.Collections.Generic;

namespace GitOut.Features.Diagnostics
{
    public class ProcessOptions
    {
        private ProcessOptions(string arguments) => Arguments = arguments;

        public string Arguments { get; }

        public static ProcessOptions FromArguments(string arguments) => new(arguments);

        public static IProcessOptionsBuilder Builder() => new ProcessOptionsBuilder();

        private class ProcessOptionsBuilder : IProcessOptionsBuilder
        {
            private readonly List<string> arguments = new();

            public IProcessOptionsBuilder Append(string argument)
            {
                arguments.Add(argument);
                return this;
            }

            public IProcessOptionsBuilder AppendRange(params string[] collection)
            {
                arguments.AddRange(collection);
                return this;
            }

            public IProcessOptionsBuilder AppendRange(IEnumerable<string> collection)
            {
                arguments.AddRange(collection);
                return this;
            }

            public ProcessOptions Build() => new(string.Join(" ", arguments));
        }
    }
}
