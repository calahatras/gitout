using System.Collections.Generic;

namespace GitOut.Features.Git
{
    public class DiffOptions
    {
        private DiffOptions(bool cached, bool ignoreAllSpace)
        {
            Cached = cached;
            IgnoreAllSpace = ignoreAllSpace;
        }

        public bool Cached { get; }
        public bool IgnoreAllSpace { get; }

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
        }

        public static IDiffOptionsBuilder Builder() => new DiffOptionsBuilder();

        private class DiffOptionsBuilder : IDiffOptionsBuilder
        {
            private bool cached;
            private bool ignoreAllSpace;

            public DiffOptions Build() => new DiffOptions(cached, ignoreAllSpace);

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
        }
    }
}
