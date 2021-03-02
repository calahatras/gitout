using System.Collections.Generic;

namespace GitOut.Features.Diagnostics
{
    public class ProcessOptions
    {
        private ProcessOptions(string arguments) => Arguments = arguments;

        public string Arguments { get; }

        public static ProcessOptions FromArguments(string arguments) => new ProcessOptions(arguments);

        public static IProcessOptionsBuilder Builder() => new ProcessOptionsBuilder();

        private class ProcessOptionsBuilder : IProcessOptionsBuilder
        {
            private readonly List<string> arguments = new List<string>();

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

            public ProcessOptions Build() => new ProcessOptions(string.Join(" ", arguments));
        }
    }
}
