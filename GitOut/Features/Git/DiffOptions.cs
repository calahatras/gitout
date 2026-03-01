using System.Collections.Generic;

namespace GitOut.Features.Git;

public class DiffOptions
{
    private DiffOptions(bool cached, bool ignoreAllSpace, bool recursive, int? contextLines)
    {
        Cached = cached;
        IgnoreAllSpace = ignoreAllSpace;
        Recursive = recursive;
        ContextLines = contextLines;
    }

    public bool Cached { get; }
    public bool IgnoreAllSpace { get; }
    public bool Recursive { get; }
    public int? ContextLines { get; }

    public IEnumerable<string> GetArguments(bool includeCached = true)
    {
        if (Cached && includeCached)
        {
            yield return "--cached";
        }
        if (IgnoreAllSpace)
        {
            yield return "--ignore-all-space";
        }
        if (Recursive)
        {
            yield return "-r";
        }
        if (ContextLines.HasValue)
        {
            yield return $"--unified={ContextLines}";
        }
    }

    public static IDiffOptionsBuilder Builder() => new DiffOptionsBuilder();

    private class DiffOptionsBuilder : IDiffOptionsBuilder
    {
        private bool cached;
        private bool ignoreAllSpace;
        private bool recursive;
        private int? contextLines;

        public DiffOptions Build() => new(cached, ignoreAllSpace, recursive, contextLines);

        public IDiffOptionsBuilder Cached()
        {
            cached = true;
            return this;
        }

        public IDiffOptionsBuilder IgnoreAllSpace()
        {
            ignoreAllSpace = true;
            return this;
        }

        public IDiffOptionsBuilder Recursive()
        {
            recursive = true;
            return this;
        }

        public IDiffOptionsBuilder ContextLines(int lines)
        {
            contextLines = lines;
            return this;
        }
    }
}
